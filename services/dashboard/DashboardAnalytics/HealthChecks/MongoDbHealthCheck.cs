using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using DashboardAnalytics.Infrastructure;

namespace DashboardAnalytics.HealthChecks;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _context;

    public MongoDbHealthCheck(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Metrics.Database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB connection is unhealthy", ex);
        }
    }
}
