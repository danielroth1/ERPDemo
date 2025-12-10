using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;
using HotChocolate.Subscriptions;
using System.Security.Claims;

namespace DashboardAnalytics.GraphQL;

public class DatabaseQuery
{
    public async Task<DatabaseOverviewResponse> GetDatabaseOverview(
        [Service] IDatabaseOverviewService service,
        bool forceRefresh = false,
        bool includeSampleDocuments = true)
    {
        var overview = await service.GetDatabaseOverviewAsync(forceRefresh);
        
        return new DatabaseOverviewResponse(
            overview.Id,
            overview.GeneratedAt,
            overview.Services.Select(MapToResponse).ToList(),
            MapStatsToResponse(overview.TotalStats),
            CacheTimeSeconds: 300
        );
    }

    public async Task<ServiceDatabaseResponse> GetServiceDatabase(
        string serviceName,
        [Service] IDatabaseOverviewService service,
        bool forceRefresh = false)
    {
        var serviceDb = await service.GetServiceDatabaseInfoAsync(serviceName, forceRefresh);
        return MapToResponse(serviceDb);
    }

    public async Task<List<DatabaseSearchResult>> SearchDatabases(
        SearchDatabaseRequest request,
        [Service] IDatabaseOverviewService service)
    {
        return await service.SearchDatabasesAsync(request);
    }

    public async Task<List<QueryExecutionHistoryResponse>> GetQueryHistory(
        [Service] IDatabaseOverviewService service,
        ClaimsPrincipal claimsPrincipal,
        int limit = 50,
        bool onlyMyQueries = false)
    {
        var userId = onlyMyQueries ? claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;
        return await service.GetQueryHistoryAsync(userId, limit);
    }

    public async Task<List<DatabaseAlertResponse>> GetDatabaseAlerts(
        [Service] IDatabaseOverviewService service,
        bool includeResolved = false)
    {
        return await service.GetAlertsAsync(includeResolved);
    }

    private static ServiceDatabaseResponse MapToResponse(ServiceDatabase service)
    {
        return new ServiceDatabaseResponse(
            service.ServiceName,
            service.DatabaseName,
            service.ConnectionString,
            service.Port,
            service.Collections.Select(MapCollectionToResponse).ToList(),
            MapStatsToResponse(service.Stats),
            service.IsConnected,
            service.ErrorMessage
        );
    }

    private static CollectionInfoResponse MapCollectionToResponse(CollectionInfo collection)
    {
        return new CollectionInfoResponse(
            collection.Name,
            collection.DocumentCount,
            collection.SizeInBytes,
            collection.AverageSizeInBytes,
            collection.Indexes.Select(MapIndexToResponse).ToList(),
            collection.SampleDocument,
            collection.Schema
        );
    }

    private static IndexInfoResponse MapIndexToResponse(IndexInfo index)
    {
        return new IndexInfoResponse(
            index.Name,
            index.Keys,
            index.IsUnique,
            index.IsSparse,
            index.SizeInBytes
        );
    }

    private static DatabaseStatsResponse MapStatsToResponse(DatabaseStats stats)
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

public class DatabaseMutation
{
    public async Task<QueryExecutionResponse> ExecuteQuery(
        ExecuteQueryRequest request,
        [Service] IDatabaseOverviewService service,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userEmail = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? "unknown";

        return await service.ExecuteQueryAsync(request, userId, userEmail);
    }

    public async Task<bool> RefreshDatabaseCache(
        [Service] IDatabaseOverviewService service,
        [Service] ITopicEventSender sender)
    {
        await service.ClearCacheAsync();
        var overview = await service.GetDatabaseOverviewAsync(forceRefresh: true);
        
        // Notify subscribers
        await sender.SendAsync("DatabaseRefreshed", new DatabaseUpdateEvent(
            ServiceName: "All",
            DatabaseName: "All",
            EventType: "CacheRefreshed",
            CollectionName: "All",
            Timestamp: DateTime.UtcNow,
            Metadata: null
        ));

        return true;
    }
}

public class DatabaseSubscription
{
    [Subscribe]
    [Topic("DatabaseUpdates")]
    public DatabaseUpdateEvent OnDatabaseUpdate(
        [EventMessage] DatabaseUpdateEvent updateEvent)
    {
        return updateEvent;
    }

    [Subscribe]
    [Topic("DatabaseRefreshed")]
    public DatabaseUpdateEvent OnDatabaseRefreshed(
        [EventMessage] DatabaseUpdateEvent updateEvent)
    {
        return updateEvent;
    }

    [Subscribe]
    [Topic("QueryExecuted")]
    public DatabaseUpdateEvent OnQueryExecuted(
        [EventMessage] DatabaseUpdateEvent updateEvent)
    {
        return updateEvent;
    }
}
