using Microsoft.EntityFrameworkCore;
using Npgsql;
using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Infrastructure;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Data;

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
    private readonly AppDbContext _dbContext;
    private readonly IPublishDatabaseUpdateService _publishService;
    
    private const string CacheKeyPrefix = "db:overview:";
    private const int CacheExpirationSeconds = 300; // 5 minutes
    
    // Define all service databases (PostgreSQL database names)
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
        AppDbContext dbContext,
        IPublishDatabaseUpdateService publishService)
    {
        _configuration = configuration;
        _logger = logger;
        _cache = cache;
        _dbContext = dbContext;
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

        var connectionString = _configuration.GetValue<string>("PostgreSQL:ConnectionString")!;
        // Build connection string for specific database
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = databaseName
        };

        var serviceDb = new ServiceDatabase
        {
            ServiceName = serviceName,
            DatabaseName = databaseName,
            ConnectionString = MaskConnectionString(builder.ConnectionString),
            Port = builder.Port
        };

        try
        {
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            serviceDb.IsConnected = true;

            // Get tables (collections equivalent) from information_schema
            var tables = new List<TableInfo>();
            
            var tableQuery = @"
                SELECT 
                    t.table_name,
                    pg_total_relation_size(quote_ident(t.table_name)) as total_size,
                    (SELECT count(*) FROM information_schema.columns c WHERE c.table_name = t.table_name AND c.table_schema = 'public') as column_count
                FROM information_schema.tables t
                WHERE t.table_schema = 'public' AND t.table_type = 'BASE TABLE'";
            
            await using var tableCmd = new NpgsqlCommand(tableQuery, connection);
            await using var tableReader = await tableCmd.ExecuteReaderAsync();
            
            var tableNames = new List<(string Name, long Size, int ColumnCount)>();
            while (await tableReader.ReadAsync())
            {
                tableNames.Add((
                    tableReader.GetString(0),
                    tableReader.GetInt64(1),
                    tableReader.GetInt32(2)
                ));
            }
            await tableReader.CloseAsync();

            // Process each table
            foreach (var (tableName, tableSize, columnCount) in tableNames)
            {
                var tableInfo = new TableInfo
                {
                    Name = tableName,
                    SizeInBytes = tableSize,
                    Indexes = await GetIndexesAsync(connection, tableName)
                };

                // Get row count
                var countQuery = $"SELECT count(*) FROM \"{tableName}\"";
                await using var countCmd = new NpgsqlCommand(countQuery, connection);
                tableInfo.RowCount = Convert.ToInt64(await countCmd.ExecuteScalarAsync() ?? 0);

                // Calculate average row size
                tableInfo.AverageSizeInBytes = tableInfo.RowCount > 0 
                    ? (double)tableInfo.SizeInBytes / tableInfo.RowCount 
                    : 0;

                // Get sample data (first 20 rows as JSON)
                var sampleQuery = $"SELECT row_to_json(t) FROM (SELECT * FROM \"{tableName}\" LIMIT 20) t";
                await using var sampleCmd = new NpgsqlCommand(sampleQuery, connection);
                await using var sampleReader = await sampleCmd.ExecuteReaderAsync();
                
                var sampleRows = new List<string>();
                while (await sampleReader.ReadAsync())
                {
                    sampleRows.Add(sampleReader.GetString(0));
                }
                await sampleReader.CloseAsync();

                if (sampleRows.Count > 0)
                {
                    tableInfo.SampleDocument = "[\n" + string.Join(",\n", sampleRows) + "\n]";
                    if (tableInfo.SampleDocument.Length > 50000)
                    {
                        tableInfo.SampleDocument = tableInfo.SampleDocument.Substring(0, 50000) + "\n... (truncated)";
                    }
                }

                // Get schema (column definitions)
                tableInfo.Schema = await GetTableSchemaAsync(connection, tableName);

                tables.Add(tableInfo);
            }

            serviceDb.Tables = tables;

            // Calculate stats
            serviceDb.Stats = new DatabaseStats
            {
                TotalCollections = serviceDb.Tables.Count,
                TotalDocuments = serviceDb.Tables.Sum(t => t.RowCount),
                TotalSizeInBytes = serviceDb.Tables.Sum(t => t.SizeInBytes),
                TotalIndexes = serviceDb.Tables.Sum(t => t.Indexes.Count),
                AverageDocumentSize = serviceDb.Tables.Count > 0 
                    ? serviceDb.Tables.Average(t => t.AverageSizeInBytes) 
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

            foreach (var table in service.Tables)
            {
                var matchedFields = new List<string>();

                // Table name filter (was CollectionName)
                if (!string.IsNullOrEmpty(request.CollectionName) &&
                    !table.Name.Contains(request.CollectionName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Row count filter (was DocumentCount)
                if (request.MinDocumentCount.HasValue && table.RowCount < request.MinDocumentCount.Value)
                {
                    continue;
                }
                if (request.MaxDocumentCount.HasValue && table.RowCount > request.MaxDocumentCount.Value)
                {
                    continue;
                }

                // Size filter
                if (request.MinSizeInBytes.HasValue && table.SizeInBytes < request.MinSizeInBytes.Value)
                {
                    continue;
                }
                if (request.MaxSizeInBytes.HasValue && table.SizeInBytes > request.MaxSizeInBytes.Value)
                {
                    continue;
                }

                // Search term in schema or sample
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var searchTerm = request.SearchTerm.ToLower();
                    
                    if (table.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedFields.Add("name");
                    }

                    if (table.Schema.Keys.Any(k => k.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                    {
                        matchedFields.AddRange(table.Schema.Keys.Where(k => 
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
                    table.Name,
                    table.RowCount,
                    table.SizeInBytes,
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
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserEmail = userEmail,
            DatabaseName = request.DatabaseName,
            CollectionName = request.CollectionName, // TableName
            Query = request.Query,
            QueryType = request.QueryType,
            ExecutedAt = DateTime.UtcNow
        };

        var results = new List<string>();

        try
        {
            // Validate query for safety
            ValidateQuery(request.Query, request.QueryType);

            var connectionString = _configuration.GetValue<string>("PostgreSQL:ConnectionString")!;
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = request.DatabaseName
            };

            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();

            switch (request.QueryType.ToLower())
            {
                case "select":
                case "find":
                    // For PostgreSQL, the query should be a SQL SELECT statement
                    // or we can construct one from a simple JSON filter
                    var selectQuery = BuildSelectQuery(request.Query, request.CollectionName, request.Limit, request.Skip);
                    await using (var cmd = new NpgsqlCommand(selectQuery, connection))
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            }
                            results.Add(JsonSerializer.Serialize(row, new JsonSerializerOptions { WriteIndented = true }));
                        }
                    }
                    break;

                case "count":
                    var countQuery = BuildCountQuery(request.Query, request.CollectionName);
                    await using (var countCmd = new NpgsqlCommand(countQuery, connection))
                    {
                        var count = await countCmd.ExecuteScalarAsync();
                        results.Add($"{{ \"count\": {count} }}");
                    }
                    break;

                case "raw":
                    // Execute raw SQL (with safety validation)
                    await using (var rawCmd = new NpgsqlCommand(request.Query, connection))
                    await using (var rawReader = await rawCmd.ExecuteReaderAsync())
                    {
                        var resultLimit = request.Limit ?? 100;
                        var rowCount = 0;
                        while (await rawReader.ReadAsync() && rowCount < resultLimit)
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < rawReader.FieldCount; i++)
                            {
                                row[rawReader.GetName(i)] = rawReader.IsDBNull(i) ? null : rawReader.GetValue(i);
                            }
                            results.Add(JsonSerializer.Serialize(row, new JsonSerializerOptions { WriteIndented = true }));
                            rowCount++;
                        }
                    }
                    break;

                default:
                    throw new ArgumentException($"Unsupported query type: {request.QueryType}. Supported types: select, find, count, raw");
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
        _dbContext.QueryExecutions.Add(execution);
        await _dbContext.SaveChangesAsync();

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

    private string BuildSelectQuery(string query, string tableName, int? limit, int? skip)
    {
        // If query looks like JSON, try to parse it as a simple filter
        if (query.TrimStart().StartsWith("{"))
        {
            try
            {
                var filter = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(query);
                var whereClauses = new List<string>();
                
                if (filter != null)
                {
                    foreach (var kvp in filter)
                    {
                        var value = kvp.Value.ValueKind == JsonValueKind.String 
                            ? $"'{kvp.Value.GetString()}'" 
                            : kvp.Value.ToString();
                        whereClauses.Add($"\"{kvp.Key}\" = {value}");
                    }
                }

                var whereClause = whereClauses.Count > 0 ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                var limitClause = limit.HasValue ? $"LIMIT {limit}" : "LIMIT 100";
                var offsetClause = skip.HasValue ? $"OFFSET {skip}" : "";
                
                return $"SELECT * FROM \"{tableName}\" {whereClause} {limitClause} {offsetClause}";
            }
            catch
            {
                // Fall through to treat as raw SQL
            }
        }

        // Treat as raw SQL or simple WHERE clause
        if (query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            return query;
        }

        // Treat query as a WHERE condition
        var wherePart = string.IsNullOrWhiteSpace(query) || query == "{}" ? "" : $"WHERE {query}";
        var limitPart = limit.HasValue ? $"LIMIT {limit}" : "LIMIT 100";
        var offsetPart = skip.HasValue ? $"OFFSET {skip}" : "";
        
        return $"SELECT * FROM \"{tableName}\" {wherePart} {limitPart} {offsetPart}";
    }

    private string BuildCountQuery(string query, string tableName)
    {
        if (query.TrimStart().StartsWith("{"))
        {
            try
            {
                var filter = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(query);
                var whereClauses = new List<string>();
                
                if (filter != null)
                {
                    foreach (var kvp in filter)
                    {
                        var value = kvp.Value.ValueKind == JsonValueKind.String 
                            ? $"'{kvp.Value.GetString()}'" 
                            : kvp.Value.ToString();
                        whereClauses.Add($"\"{kvp.Key}\" = {value}");
                    }
                }

                var whereClause = whereClauses.Count > 0 ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                return $"SELECT COUNT(*) FROM \"{tableName}\" {whereClause}";
            }
            catch
            {
                // Fall through
            }
        }

        var wherePart = string.IsNullOrWhiteSpace(query) || query == "{}" ? "" : $"WHERE {query}";
        return $"SELECT COUNT(*) FROM \"{tableName}\" {wherePart}";
    }

    public async Task<List<QueryExecutionHistoryResponse>> GetQueryHistoryAsync(string? userId = null, int limit = 50)
    {
        var query = _dbContext.QueryExecutions.AsQueryable();
        
        if (userId != null)
        {
            query = query.Where(q => q.UserId == userId);
        }

        var executions = await query
            .OrderByDescending(q => q.ExecutedAt)
            .Take(limit)
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
        var query = _dbContext.DatabaseAlerts.AsQueryable();
        
        if (!includeResolved)
        {
            query = query.Where(a => !a.IsResolved);
        }

        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
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
    private async Task<List<IndexInfo>> GetIndexesAsync(NpgsqlConnection connection, string tableName)
    {
        var indexes = new List<IndexInfo>();
        
        var indexQuery = @"
            SELECT 
                i.relname as index_name,
                a.attname as column_name,
                ix.indisunique as is_unique,
                am.amname as index_type
            FROM pg_index ix
            JOIN pg_class i ON i.oid = ix.indexrelid
            JOIN pg_class t ON t.oid = ix.indrelid
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
            JOIN pg_am am ON am.oid = i.relam
            WHERE t.relname = @tableName AND t.relnamespace = 'public'::regnamespace
            ORDER BY i.relname, a.attnum";

        await using var cmd = new NpgsqlCommand(indexQuery, connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        
        var indexDict = new Dictionary<string, IndexInfo>();
        while (await reader.ReadAsync())
        {
            var indexName = reader.GetString(0);
            var columnName = reader.GetString(1);
            var isUnique = reader.GetBoolean(2);
            
            if (!indexDict.TryGetValue(indexName, out var indexInfo))
            {
                indexInfo = new IndexInfo
                {
                    Name = indexName,
                    Keys = new Dictionary<string, int>(),
                    IsUnique = isUnique,
                    IsSparse = false // PostgreSQL doesn't have sparse indexes in the same way
                };
                indexDict[indexName] = indexInfo;
            }
            
            indexInfo.Keys[columnName] = 1; // Direction (1 for ASC, -1 for DESC)
        }

        return indexDict.Values.ToList();
    }

    private async Task<Dictionary<string, string>> GetTableSchemaAsync(NpgsqlConnection connection, string tableName)
    {
        var schema = new Dictionary<string, string>();
        
        var schemaQuery = @"
            SELECT column_name, data_type, is_nullable
            FROM information_schema.columns 
            WHERE table_name = @tableName AND table_schema = 'public'
            ORDER BY ordinal_position";

        await using var cmd = new NpgsqlCommand(schemaQuery, connection);
        cmd.Parameters.AddWithValue("tableName", tableName);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString(0);
            var dataType = reader.GetString(1);
            var isNullable = reader.GetString(2);
            schema[columnName] = $"{dataType}{(isNullable == "YES" ? " (nullable)" : "")}";
        }

        return schema;
    }

    private void ValidateQuery(string query, string queryType)
    {
        // Prevent dangerous operations for non-raw queries
        var dangerousKeywords = new[] { "drop", "truncate", "delete", "insert", "update", "alter", "create", "grant", "revoke" };
        var lowerQuery = query.ToLower();

        // For raw queries, still prevent destructive operations
        if (queryType.ToLower() == "raw")
        {
            var destructiveKeywords = new[] { "drop", "truncate", "delete", "alter", "create", "grant", "revoke" };
            foreach (var keyword in destructiveKeywords)
            {
                if (lowerQuery.Contains(keyword))
                {
                    throw new InvalidOperationException($"Query contains forbidden keyword: {keyword}");
                }
            }
        }
        else
        {
            foreach (var keyword in dangerousKeywords)
            {
                if (lowerQuery.Contains(keyword))
                {
                    throw new InvalidOperationException($"Query contains forbidden keyword: {keyword}");
                }
            }
        }

        // For JSON queries, validate JSON format
        if (query.TrimStart().StartsWith("{"))
        {
            try
            {
                JsonDocument.Parse(query);
            }
            catch
            {
                throw new ArgumentException("Invalid JSON query");
            }
        }
    }

    private async Task CheckAndCreateAlertsAsync(DatabaseOverview overview)
    {
        foreach (var service in overview.Services.Where(s => s.IsConnected))
        {
            foreach (var table in service.Tables)
            {
                // Check for high row count
                if (table.RowCount > 1000000)
                {
                    await CreateAlertIfNotExistsAsync(
                        service.ServiceName,
                        service.DatabaseName,
                        table.Name,
                        "HighRowCount",
                        $"Table {table.Name} has {table.RowCount:N0} rows",
                        "Warning"
                    );
                }

                // Check for large table size
                if (table.SizeInBytes > 1_000_000_000) // 1GB
                {
                    await CreateAlertIfNotExistsAsync(
                        service.ServiceName,
                        service.DatabaseName,
                        table.Name,
                        "LargeTableSize",
                        $"Table {table.Name} is {table.SizeInBytes / 1_000_000_000.0:F2} GB",
                        "Warning"
                    );
                }
            }
        }
    }

    private async Task CreateAlertIfNotExistsAsync(
        string serviceName, 
        string databaseName, 
        string tableName,
        string alertType,
        string message,
        string severity)
    {
        var existing = await _dbContext.DatabaseAlerts
            .Where(a => a.ServiceName == serviceName 
                     && a.CollectionName == tableName 
                     && a.AlertType == alertType 
                     && !a.IsResolved)
            .FirstOrDefaultAsync();

        if (existing == null)
        {
            var alert = new DatabaseAlert
            {
                Id = Guid.NewGuid().ToString(),
                ServiceName = serviceName,
                DatabaseName = databaseName,
                CollectionName = tableName,
                AlertType = alertType,
                Message = message,
                Severity = severity,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.DatabaseAlerts.Add(alert);
            await _dbContext.SaveChangesAsync();
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            if (!string.IsNullOrEmpty(builder.Password))
            {
                builder.Password = "****";
            }
            return builder.ConnectionString;
        }
        catch
        {
            return "****";
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
