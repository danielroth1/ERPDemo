using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using UserManagement.Configuration;

namespace UserManagement.HealthChecks;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly IMongoClient _client;

    public MongoDbHealthCheck(MongoDbSettings settings)
    {
        _client = new MongoClient(settings.ConnectionString);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.ListDatabaseNamesAsync(cancellationToken);
            return HealthCheckResult.Healthy("MongoDB connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB connection failed", ex);
        }
    }
}
