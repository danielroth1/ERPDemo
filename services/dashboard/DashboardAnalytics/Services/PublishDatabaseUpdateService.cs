using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;

namespace DashboardAnalytics.Services;

public interface IPublishDatabaseUpdateService
{
    Task PublishDatabaseUpdateAsync(DatabaseUpdateEvent updateEvent);
    Task PublishQueryExecutedAsync(QueryExecution execution);
    Task PublishCollectionChangedAsync(string serviceName, string databaseName, string collectionName, string eventType);
}

public class PublishDatabaseUpdateService : IPublishDatabaseUpdateService
{
    private readonly ILogger<PublishDatabaseUpdateService> _logger;
    private readonly List<Func<DatabaseUpdateEvent, Task>> _subscribers = new();

    public PublishDatabaseUpdateService(ILogger<PublishDatabaseUpdateService> logger)
    {
        _logger = logger;
    }

    public void Subscribe(Func<DatabaseUpdateEvent, Task> handler)
    {
        _subscribers.Add(handler);
    }

    public async Task PublishDatabaseUpdateAsync(DatabaseUpdateEvent updateEvent)
    {
        _logger.LogInformation("Publishing database update event: {EventType} for {CollectionName}", 
            updateEvent.EventType, updateEvent.CollectionName);

        var tasks = _subscribers.Select(subscriber => 
        {
            try
            {
                return subscriber(updateEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscriber");
                return Task.CompletedTask;
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task PublishQueryExecutedAsync(QueryExecution execution)
    {
        var updateEvent = new DatabaseUpdateEvent(
            ServiceName: "Dashboard",
            DatabaseName: execution.DatabaseName,
            EventType: "QueryExecuted",
            CollectionName: execution.CollectionName,
            Timestamp: execution.ExecutedAt,
            Metadata: new Dictionary<string, object>
            {
                { "userId", execution.UserId },
                { "userEmail", execution.UserEmail },
                { "queryType", execution.QueryType },
                { "isSuccessful", execution.IsSuccessful },
                { "executionTimeMs", execution.ExecutionTimeMs },
                { "resultCount", execution.ResultCount }
            }
        );

        await PublishDatabaseUpdateAsync(updateEvent);
    }

    public async Task PublishCollectionChangedAsync(
        string serviceName, 
        string databaseName, 
        string collectionName, 
        string eventType)
    {
        var updateEvent = new DatabaseUpdateEvent(
            ServiceName: serviceName,
            DatabaseName: databaseName,
            EventType: eventType,
            CollectionName: collectionName,
            Timestamp: DateTime.UtcNow,
            Metadata: null
        );

        await PublishDatabaseUpdateAsync(updateEvent);
    }
}
