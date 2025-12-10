using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using FinancialManagement.Infrastructure;

namespace FinancialManagement.HealthChecks;

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
            await _context.Accounts.Database.ListCollectionNamesAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("MongoDB connection is healthy");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MongoDB health check failed");
            return HealthCheckResult.Unhealthy("MongoDB connection is unhealthy", ex);
        }
    }
}
