using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Infrastructure;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace DashboardAnalytics.Services;

public interface IDatabaseOverviewService
{
    Task<DatabaseOverview> GetDatabaseOverviewAsync(bool forceRefresh = false);
    Task<ServiceDatabase> GetServiceDatabaseInfoAsync(string serviceName, bool forceRefresh = false);
    Task<List<DatabaseSearchResult>> SearchDatabasesAsync(SearchDatabaseRequest request);
    Task<QueryExecutionResponse> ExecuteQueryAsync(ExecuteQueryRequest request, string userId, string userEmail);
    Task<List<QueryExecutionHistoryResponse>> GetQueryHistoryAsync(string? userId = null, int limit = 50);
    Task<List<DatabaseAlertResponse>> GetAlertsAsync(bool includeResolved = false);
    Task ClearCacheAsync();
}

public class DatabaseOverviewService : IDatabaseOverviewService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseOverviewService> _logger;
    private readonly IDistributedCache _cache;
    private readonly MongoDbContext _context;
    private readonly IPublishDatabaseUpdateService _publishService;
    
    private const string CacheKeyPrefix = "db:overview:";
    private const int CacheExpirationSeconds = 300; // 5 minutes
    
    // Define all service databases
    private readonly Dictionary<string, string> _serviceDatabases = new()
    {
        { "User Management", "erp_users" },
        { "Inventory", "erp_inventory" },
        { "Sales", "erp_sales" },
        { "Financial", "erp_financial" },
        { "Dashboard", "erp_dashboard" }
    };

    public DatabaseOverviewService(
        IConfiguration configuration,
        ILogger<DatabaseOverviewService> logger,
        IDistributedCache cache,
        MongoDbContext context,
        IPublishDatabaseUpdateService publishService)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _context = context;
        _publishService = publishService;
    }

    public async Task<DatabaseOverview> GetDatabaseOverviewAsync(bool forceRefresh = false)
    {
        var cacheKey = $"{CacheKeyPrefix}all";

        if (!forceRefresh)
        {
            var cached = await GetFromCacheAsync<DatabaseOverview>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Returning cached database overview");
                return cached;
            }
        }

        var overview = new DatabaseOverview();
        var totalStats = new DatabaseStats();

        foreach (var kvp in _serviceDatabases)
        {
            try
            {
                var serviceDb = await GetServiceDatabaseInfoInternalAsync(kvp.Key, forceRefresh);
                overview.Services.Add(serviceDb);

                if (serviceDb.IsConnected)
                {
                    totalStats.TotalCollections += serviceDb.Stats.TotalCollections;
                    totalStats.TotalDocuments += serviceDb.Stats.TotalDocuments;
                    totalStats.TotalSizeInBytes += serviceDb.Stats.TotalSizeInBytes;
                    totalStats.TotalIndexes += serviceDb.Stats.TotalIndexes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get database info for {ServiceName}", kvp.Key);
                overview.Services.Add(new ServiceDatabase
                {
                    ServiceName = kvp.Key,
                    DatabaseName = kvp.Value,
                    IsConnected = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        if (totalStats.TotalDocuments > 0)
        {
            totalStats.AverageDocumentSize = (double)totalStats.TotalSizeInBytes / totalStats.TotalDocuments;
        }

        overview.TotalStats = totalStats;

        // Cache the result
        await SetCacheAsync(cacheKey, overview, CacheExpirationSeconds);

        // Check for alerts
        await CheckAndCreateAlertsAsync(overview);

        return overview;
    }

    public async Task<ServiceDatabase> GetServiceDatabaseInfoAsync(string serviceName, bool forceRefresh = false)
    {
        var cacheKey = $"{CacheKeyPrefix}service:{serviceName}";

        if (!forceRefresh)
        {
            var cached = await GetFromCacheAsync<ServiceDatabase>(cacheKey);
            if (cached != null)
            {
                return cached;
            }
        }

        var result = await GetServiceDatabaseInfoInternalAsync(serviceName, forceRefresh);
        await SetCacheAsync(cacheKey, result, CacheExpirationSeconds);
        return result;
    }

    private async Task<ServiceDatabase> GetServiceDatabaseInfoInternalAsync(string serviceName, bool forceRefresh)
    {
        if (!_serviceDatabases.TryGetValue(serviceName, out var databaseName))
        {
            throw new ArgumentException($"Unknown service: {serviceName}");
        }

        var connectionString = _configuration.GetValue<string>("MongoDb:ConnectionString")!;
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        var serviceDb = new ServiceDatabase
        {
            ServiceName = serviceName,
            DatabaseName = databaseName,
            ConnectionString = MaskConnectionString(connectionString),
            Port = ExtractPort(connectionString)
        };

        try
        {
            // Test connection
            await database.ListCollectionNamesAsync();
            serviceDb.IsConnected = true;

            // Get collections
            var collectionNames = await database.ListCollectionNamesAsync();
            var collectionNamesList = await collectionNames.ToListAsync();
            
            // Process collections in parallel for better performance
            var collectionTasks = collectionNamesList.Select(async collectionName =>
            {
                var collection = database.GetCollection<BsonDocument>(collectionName);
                
                var collectionInfo = new CollectionInfo
                {
                    Name = collectionName,
                    DocumentCount = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty),
                    Indexes = await GetIndexesAsync(collection)
                };

                // Get collection stats
                try
                {
                    var command = new BsonDocument { { "collStats", collectionName } };
                    var stats = await database.RunCommandAsync<BsonDocument>(command);
                    collectionInfo.SizeInBytes = stats.GetValue("size", 0).ToInt64();
                    collectionInfo.AverageSizeInBytes = stats.Contains("avgObjSize") 
                        ? stats.GetValue("avgObjSize").ToDouble() 
                        : 0;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get stats for collection {CollectionName}", collectionName);
                }

                // Get sample document (first one)
                var sampleDocs = await collection.Find(FilterDefinition<BsonDocument>.Empty)
                    .Limit(20)
                    .ToListAsync();
                
                if (sampleDocs != null && sampleDocs.Count > 0)
                {
                    // Format as JSON array with proper indentation
                    var jsonArray = new BsonArray(sampleDocs);
                    var json = jsonArray.ToJson(new MongoDB.Bson.IO.JsonWriterSettings 
                    { 
                        Indent = true
                    });
                    // Truncate if too long
                    collectionInfo.SampleDocument = json.Length > 50000 ? json.Substring(0, 50000) + "\n... (truncated)" : json;

                    // Infer schema from first document
                    collectionInfo.Schema = InferSchema(sampleDocs[0]);
                }

                return collectionInfo;
            });

            var collections = (await Task.WhenAll(collectionTasks)).ToList();

            serviceDb.Collections = collections;

            // Calculate stats
            serviceDb.Stats = new DatabaseStats
            {
                TotalCollections = serviceDb.Collections.Count,
                TotalDocuments = serviceDb.Collections.Sum(c => c.DocumentCount),
                TotalSizeInBytes = serviceDb.Collections.Sum(c => c.SizeInBytes),
                TotalIndexes = serviceDb.Collections.Sum(c => c.Indexes.Count),
                AverageDocumentSize = serviceDb.Collections.Count > 0 
                    ? serviceDb.Collections.Average(c => c.AverageSizeInBytes) 
                    : 0
            };
        }
        catch (Exception ex)
        {
            serviceDb.IsConnected = false;
            serviceDb.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to connect to database {DatabaseName}", databaseName);
        }

        return serviceDb;
    }

    public async Task<List<DatabaseSearchResult>> SearchDatabasesAsync(SearchDatabaseRequest request)
    {
        var overview = await GetDatabaseOverviewAsync();
        var results = new List<DatabaseSearchResult>();

        foreach (var service in overview.Services.Where(s => s.IsConnected))
        {
            if (!string.IsNullOrEmpty(request.ServiceName) && 
                !service.ServiceName.Contains(request.ServiceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var collection in service.Collections)
            {
                var matchedFields = new List<string>();

                // Collection name filter
                if (!string.IsNullOrEmpty(request.CollectionName) &&
                    !collection.Name.Contains(request.CollectionName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Document count filter
                if (request.MinDocumentCount.HasValue && collection.DocumentCount < request.MinDocumentCount.Value)
                {
                    continue;
                }
                if (request.MaxDocumentCount.HasValue && collection.DocumentCount > request.MaxDocumentCount.Value)
                {
                    continue;
                }

                // Size filter
                if (request.MinSizeInBytes.HasValue && collection.SizeInBytes < request.MinSizeInBytes.Value)
                {
                    continue;
                }
                if (request.MaxSizeInBytes.HasValue && collection.SizeInBytes > request.MaxSizeInBytes.Value)
                {
                    continue;
                }

                // Search term in schema or sample
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    
                    if (collection.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedFields.Add("name");
                    }

                    if (collection.Schema.Keys.Any(k => k.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                    {
                        matchedFields.AddRange(collection.Schema.Keys.Where(k => 
                            k.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                    }

                    if (matchedFields.Count == 0)
                    {
                        continue;
                    }
                }

                results.Add(new DatabaseSearchResult(
                    service.ServiceName,
                    service.DatabaseName,
                    collection.Name,
                    collection.DocumentCount,
                    collection.SizeInBytes,
                    matchedFields
                ));
            }
        }

        return results;
    }

    public async Task<QueryExecutionResponse> ExecuteQueryAsync(
        ExecuteQueryRequest request, 
        string userId, 
        string userEmail)
    {
        var stopwatch = Stopwatch.StartNew();
        var execution = new QueryExecution
        {
            UserId = userId,
            UserEmail = userEmail,
            DatabaseName = request.DatabaseName,
            CollectionName = request.CollectionName,
            Query = request.Query,
            QueryType = request.QueryType
        };

        var results = new List<string>();

        try
        {
            // Validate query for safety
            ValidateQuery(request.Query);

            var connectionString = _configuration.GetValue<string>("MongoDb:ConnectionString")!;
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(request.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(request.CollectionName);

            switch (request.QueryType.ToLower())
            {
                case "find":
                    var filter = BsonDocument.Parse(request.Query);
                    var cursor = await collection.FindAsync(filter, new FindOptions<BsonDocument>
                    {
                        Limit = request.Limit,
                        Skip = request.Skip
                    });
                    var documents = await cursor.ToListAsync();
                    results = documents.Select(d => d.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true })).ToList();
                    break;

                case "count":
                    var countFilter = BsonDocument.Parse(request.Query);
                    var count = await collection.CountDocumentsAsync(countFilter);
                    results.Add($"{{ \"count\": {count} }}");
                    break;

                case "aggregate":
                    var pipeline = BsonSerializer.Deserialize<BsonDocument[]>(request.Query);
                    var aggregateCursor = await collection.AggregateAsync<BsonDocument>(pipeline);
                    var aggregateResults = await aggregateCursor.ToListAsync();
                    results = aggregateResults.Take(request.Limit ?? 100)
                        .Select(d => d.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true })).ToList();
                    break;

                default:
                    throw new ArgumentException($"Unsupported query type: {request.QueryType}");
            }

            execution.IsSuccessful = true;
            execution.ResultCount = results.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed for user {UserEmail}", userEmail);
            execution.IsSuccessful = false;
            execution.ErrorMessage = ex.Message;
        }

        stopwatch.Stop();
        execution.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        // Save execution history
        await _context.QueryExecutions.InsertOneAsync(execution);

        // Publish event
        await _publishService.PublishQueryExecutedAsync(execution);

        return new QueryExecutionResponse(
            execution.Id,
            execution.IsSuccessful,
            execution.ErrorMessage,
            results,
            execution.ResultCount,
            execution.ExecutionTimeMs,
            execution.ExecutedAt
        );
    }

    public async Task<List<QueryExecutionHistoryResponse>> GetQueryHistoryAsync(string? userId = null, int limit = 50)
    {
        var filter = userId != null 
            ? Builders<QueryExecution>.Filter.Eq(q => q.UserId, userId)
            : Builders<QueryExecution>.Filter.Empty;

        var executions = await _context.QueryExecutions
            .Find(filter)
            .SortByDescending(q => q.ExecutedAt)
            .Limit(limit)
            .ToListAsync();

        return executions.Select(e => new QueryExecutionHistoryResponse(
            e.Id,
            e.UserEmail,
            e.DatabaseName,
            e.CollectionName,
            e.Query,
            e.QueryType,
            e.IsSuccessful,
            e.ResultCount,
            e.ExecutionTimeMs,
            e.ExecutedAt
        )).ToList();
    }

    public async Task<List<DatabaseAlertResponse>> GetAlertsAsync(bool includeResolved = false)
    {
        var filter = includeResolved
            ? Builders<DatabaseAlert>.Filter.Empty
            : Builders<DatabaseAlert>.Filter.Eq(a => a.IsResolved, false);

        var alerts = await _context.DatabaseAlerts
            .Find(filter)
            .SortByDescending(a => a.CreatedAt)
            .Limit(100)
            .ToListAsync();

        return alerts.Select(a => new DatabaseAlertResponse(
            a.Id,
            a.ServiceName,
            a.DatabaseName,
            a.CollectionName,
            a.AlertType,
            a.Message,
            a.Severity,
            a.Metadata,
            a.IsResolved,
            a.CreatedAt,
            a.ResolvedAt
        )).ToList();
    }

    public async Task ClearCacheAsync()
    {
        // Note: IDistributedCache doesn't have a clear all method
        // You'd need to track keys or use Redis directly for bulk operations
        _logger.LogInformation("Cache clear requested - implement Redis SCAN for production");
        
        // Clear known keys
        foreach (var service in _serviceDatabases.Keys)
        {
            await _cache.RemoveAsync($"{CacheKeyPrefix}service:{service}");
        }
        await _cache.RemoveAsync($"{CacheKeyPrefix}all");
    }

    // Helper methods
    private async Task<List<IndexInfo>> GetIndexesAsync(IMongoCollection<BsonDocument> collection)
    {
        var indexes = new List<IndexInfo>();
        var cursor = await collection.Indexes.ListAsync();
        
        await cursor.ForEachAsync(index =>
        {
            var indexInfo = new IndexInfo
            {
                Name = index.GetValue("name").AsString,
                Keys = new Dictionary<string, int>(),
                IsUnique = index.Contains("unique") && index.GetValue("unique").AsBoolean,
                IsSparse = index.Contains("sparse") && index.GetValue("sparse").AsBoolean
            };

            var keyDoc = index.GetValue("key").AsBsonDocument;
            foreach (var element in keyDoc.Elements)
            {
                indexInfo.Keys[element.Name] = element.Value.ToInt32();
            }

            indexes.Add(indexInfo);
        });

        return indexes;
    }

    private Dictionary<string, string> InferSchema(BsonDocument document)
    {
        var schema = new Dictionary<string, string>();
        
        foreach (var element in document.Elements)
        {
            schema[element.Name] = element.Value.BsonType.ToString();
        }

        return schema;
    }

    private void ValidateQuery(string query)
    {
        // Prevent dangerous operations
        var dangerousKeywords = new[] { "$where", "eval", "function", "javascript" };
        var lowerQuery = query.ToLower();

        foreach (var keyword in dangerousKeywords)
        {
            if (lowerQuery.Contains(keyword))
            {
                throw new InvalidOperationException($"Query contains forbidden keyword: {keyword}");
            }
        }

        // Validate JSON
        try
        {
            JsonDocument.Parse(query);
        }
        catch
        {
            throw new ArgumentException("Invalid JSON query");
        }
    }

    private async Task CheckAndCreateAlertsAsync(DatabaseOverview overview)
    {
        foreach (var service in overview.Services.Where(s => s.IsConnected))
        {
            foreach (var collection in service.Collections)
            {
                // Check for high document count
                if (collection.DocumentCount > 1000000)
                {
                    await CreateAlertIfNotExistsAsync(
                        service.ServiceName,
                        service.DatabaseName,
                        collection.Name,
                        "HighDocumentCount",
                        $"Collection {collection.Name} has {collection.DocumentCount:N0} documents",
                        "Warning"
                    );
                }

                // Check for large collection size
                if (collection.SizeInBytes > 1_000_000_000) // 1GB
                {
                    await CreateAlertIfNotExistsAsync(
                        service.ServiceName,
                        service.DatabaseName,
                        collection.Name,
                        "LargeCollectionSize",
                        $"Collection {collection.Name} is {collection.SizeInBytes / 1_000_000_000.0:F2} GB",
                        "Warning"
                    );
                }
            }
        }
    }

    private async Task CreateAlertIfNotExistsAsync(
        string serviceName, 
        string databaseName, 
        string collectionName,
        string alertType,
        string message,
        string severity)
    {
        var filter = Builders<DatabaseAlert>.Filter.And(
            Builders<DatabaseAlert>.Filter.Eq(a => a.ServiceName, serviceName),
            Builders<DatabaseAlert>.Filter.Eq(a => a.CollectionName, collectionName),
            Builders<DatabaseAlert>.Filter.Eq(a => a.AlertType, alertType),
            Builders<DatabaseAlert>.Filter.Eq(a => a.IsResolved, false)
        );

        var existing = await _context.DatabaseAlerts.Find(filter).FirstOrDefaultAsync();
        if (existing == null)
        {
            var alert = new DatabaseAlert
            {
                ServiceName = serviceName,
                DatabaseName = databaseName,
                CollectionName = collectionName,
                AlertType = alertType,
                Message = message,
                Severity = severity
            };

            await _context.DatabaseAlerts.InsertOneAsync(alert);
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        try
        {
            var uri = new MongoUrl(connectionString);
            return connectionString.Contains("@") && !string.IsNullOrEmpty(uri.Password)
                ? connectionString.Replace(uri.Password, "****") 
                : connectionString;
        }
        catch
        {
            return "****";
        }
    }

    private static int ExtractPort(string connectionString)
    {
        try
        {
            var uri = new MongoUrl(connectionString);
            return uri.Server.Port;
        }
        catch
        {
            return 27017;
        }
    }

    private async Task<T?> GetFromCacheAsync<T>(string key) where T : class
    {
        try
        {
            var cached = await _cache.GetAsync(key);
            if (cached == null) return null;

            var json = Encoding.UTF8.GetString(cached);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read from cache for key: {CacheKey}. Continuing without cache.", key);
            return null;
        }
    }

    private async Task SetCacheAsync<T>(string key, T value, int expirationSeconds)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(expirationSeconds)
            };

            await _cache.SetAsync(key, bytes, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write to cache for key: {CacheKey}. Continuing without cache.", key);
        }
    }
}
