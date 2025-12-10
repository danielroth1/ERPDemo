using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DashboardAnalytics.Services;
using DashboardAnalytics.Models.DTOs;
using System.Security.Claims;

namespace DashboardAnalytics.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseOverviewService _databaseService;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(
        IDatabaseOverviewService databaseService,
        ILogger<DatabaseController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete database overview for all services
    /// </summary>
    [HttpGet("overview")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(DatabaseOverviewResponse), 200)]
    public async Task<IActionResult> GetDatabaseOverview(
        [FromQuery] bool forceRefresh = false,
        [FromQuery] bool includeSampleDocuments = true)
    {
        try
        {
            var overview = await _databaseService.GetDatabaseOverviewAsync(forceRefresh);
            
            var response = new DatabaseOverviewResponse(
                overview.Id,
                overview.GeneratedAt,
                overview.Services.Select(MapToServiceResponse).ToList(),
                MapToStatsResponse(overview.TotalStats),
                CacheTimeSeconds: 300
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database overview");
            return StatusCode(500, new { error = "Failed to retrieve database overview" });
        }
    }

    /// <summary>
    /// Get database info for specific service
    /// </summary>
    [HttpGet("service/{serviceName}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ServiceDatabaseResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetServiceDatabase(
        string serviceName,
        [FromQuery] bool forceRefresh = false)
    {
        try
        {
            var serviceDb = await _databaseService.GetServiceDatabaseInfoAsync(serviceName, forceRefresh);
            var response = MapToServiceResponse(serviceDb);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service database for {ServiceName}", serviceName);
            return StatusCode(500, new { error = "Failed to retrieve service database" });
        }
    }

    /// <summary>
    /// Search across all databases and collections
    /// </summary>
    [HttpPost("search")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(List<DatabaseSearchResult>), 200)]
    public async Task<IActionResult> SearchDatabases([FromBody] SearchDatabaseRequest request)
    {
        try
        {
            var results = await _databaseService.SearchDatabasesAsync(request);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search databases");
            return StatusCode(500, new { error = "Search failed" });
        }
    }

    /// <summary>
    /// Execute a MongoDB query (Admin only)
    /// </summary>
    [HttpPost("query")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(QueryExecutionResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ExecuteQuery([FromBody] ExecuteQueryRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

            var result = await _databaseService.ExecuteQueryAsync(request, userId, userEmail);
            
            if (!result.IsSuccessful)
            {
                return BadRequest(new 
                { 
                    error = result.ErrorMessage,
                    executionTimeMs = result.ExecutionTimeMs 
                });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed");
            return StatusCode(500, new { error = "Query execution failed" });
        }
    }

    /// <summary>
    /// Get query execution history
    /// </summary>
    [HttpGet("query-history")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(List<QueryExecutionHistoryResponse>), 200)]
    public async Task<IActionResult> GetQueryHistory(
        [FromQuery] bool onlyMyQueries = false,
        [FromQuery] int limit = 50)
    {
        try
        {
            var userId = onlyMyQueries 
                ? User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                : null;

            var history = await _databaseService.GetQueryHistoryAsync(userId, limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get query history");
            return StatusCode(500, new { error = "Failed to retrieve query history" });
        }
    }

    /// <summary>
    /// Get database alerts
    /// </summary>
    [HttpGet("alerts")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(List<DatabaseAlertResponse>), 200)]
    public async Task<IActionResult> GetAlerts([FromQuery] bool includeResolved = false)
    {
        try
        {
            var alerts = await _databaseService.GetAlertsAsync(includeResolved);
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alerts");
            return StatusCode(500, new { error = "Failed to retrieve alerts" });
        }
    }

    /// <summary>
    /// Clear cache and force refresh
    /// </summary>
    [HttpPost("cache/clear")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ClearCache()
    {
        try
        {
            await _databaseService.ClearCacheAsync();
            return Ok(new { message = "Cache cleared successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
            return StatusCode(500, new { error = "Failed to clear cache" });
        }
    }

    // Helper mapping methods
    private static ServiceDatabaseResponse MapToServiceResponse(Models.ServiceDatabase service)
    {
        return new ServiceDatabaseResponse(
            service.ServiceName,
            service.DatabaseName,
            service.ConnectionString,
            service.Port,
            service.Collections.Select(MapToCollectionResponse).ToList(),
            MapToStatsResponse(service.Stats),
            service.IsConnected,
            service.ErrorMessage
        );
    }

    private static CollectionInfoResponse MapToCollectionResponse(Models.CollectionInfo collection)
    {
        return new CollectionInfoResponse(
            collection.Name,
            collection.DocumentCount,
            collection.SizeInBytes,
            collection.AverageSizeInBytes,
            collection.Indexes.Select(MapToIndexResponse).ToList(),
            collection.SampleDocument,
            collection.Schema
        );
    }

    private static IndexInfoResponse MapToIndexResponse(Models.IndexInfo index)
    {
        return new IndexInfoResponse(
            index.Name,
            index.Keys,
            index.IsUnique,
            index.IsSparse,
            index.SizeInBytes
        );
    }

    private static DatabaseStatsResponse MapToStatsResponse(Models.DatabaseStats stats)
    {
        return new DatabaseStatsResponse(
            stats.TotalCollections,
            stats.TotalDocuments,
            stats.TotalSizeInBytes,
            stats.TotalIndexes,
            stats.AverageDocumentSize
        );
    }
}
