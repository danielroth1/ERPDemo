using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using SalesManagement.Infrastructure;

namespace SalesManagement.HealthChecks;

public class MongoDbHealthCheck : IHealthCheck
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MongoDbHealthCheck> _logger;

    public MongoDbHealthCheck(MongoDbContext context, ILogger<MongoDbHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to access the database
            await _context.Orders.Database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB connection is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy("MongoDB connection is unhealthy", ex);
        }
    }
}
