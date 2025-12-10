using MongoDB.Driver;
using FinancialManagement.Configuration;
using FinancialManagement.Models;

namespace FinancialManagement.Infrastructure;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(MongoDbSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<Account> Accounts => _database.GetCollection<Account>("accounts");
    public IMongoCollection<Transaction> Transactions => _database.GetCollection<Transaction>("transactions");
    public IMongoCollection<Budget> Budgets => _database.GetCollection<Budget>("budgets");
}
