using MongoDB.Driver;
using DashboardAnalytics.Configuration;
using DashboardAnalytics.Models;
using Microsoft.Extensions.Options;

namespace DashboardAnalytics.Infrastructure;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<DashboardMetrics> Metrics => 
        _database.GetCollection<DashboardMetrics>("metrics");

    public IMongoCollection<KPI> KPIs => 
        _database.GetCollection<KPI>("kpis");

    public IMongoCollection<ChartData> Charts => 
        _database.GetCollection<ChartData>("charts");

    public IMongoCollection<Alert> Alerts => 
        _database.GetCollection<Alert>("alerts");

    public IMongoCollection<QueryExecution> QueryExecutions => 
        _database.GetCollection<QueryExecution>("query_executions");

    public IMongoCollection<DatabaseAlert> DatabaseAlerts => 
        _database.GetCollection<DatabaseAlert>("database_alerts");
}
