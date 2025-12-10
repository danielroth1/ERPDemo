using MongoDB.Driver;
using FinancialManagement.Infrastructure;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;

namespace FinancialManagement.Services;

public interface IBudgetService
{
    Task<BudgetResponse> CreateBudgetAsync(CreateBudgetRequest request);
    Task<BudgetResponse?> GetBudgetByIdAsync(string id);
    Task<List<BudgetResponse>> GetAllBudgetsAsync(int skip = 0, int limit = 100);
    Task<List<BudgetResponse>> GetActiveBudgetsAsync(int skip = 0, int limit = 100);
    Task<List<BudgetResponse>> GetBudgetsByAccountAsync(string accountId, int skip = 0, int limit = 100);
    Task<BudgetResponse?> UpdateBudgetAsync(string id, UpdateBudgetRequest request);
    Task<bool> DeleteBudgetAsync(string id);
    Task UpdateBudgetSpendingAsync(string accountId, decimal amount);
}

public class BudgetService : IBudgetService
{
    private readonly MongoDbContext _context;
    private readonly IAccountService _accountService;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<BudgetService> _logger;
    private const string FinancialTopic = "financial-events";

    public BudgetService(
        MongoDbContext context,
        IAccountService accountService,
        KafkaProducer kafkaProducer,
        ILogger<BudgetService> logger)
    {
        _context = context;
        _accountService = accountService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<BudgetResponse> CreateBudgetAsync(CreateBudgetRequest request)
    {
        // Validate account exists
        var account = await _accountService.GetAccountByIdAsync(request.AccountId);
        if (account == null)
        {
            throw new ArgumentException($"Account {request.AccountId} not found");
        }

        if (!Enum.TryParse<BudgetPeriod>(request.Period, true, out var period))
        {
            throw new ArgumentException($"Invalid budget period: {request.Period}");
        }

        var budget = new Budget
        {
            Name = request.Name,
            AccountId = request.AccountId,
            Period = period,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Amount = request.Amount,
            Spent = 0,
            Remaining = request.Amount
        };

        await _context.Budgets.InsertOneAsync(budget);

        _logger.LogInformation("Created budget {Name} for account {AccountId}", request.Name, request.AccountId);

        return MapToResponse(budget);
    }

    public async Task<BudgetResponse?> GetBudgetByIdAsync(string id)
    {
        var budget = await _context.Budgets.Find(b => b.Id == id).FirstOrDefaultAsync();
        return budget != null ? MapToResponse(budget) : null;
    }

    public async Task<List<BudgetResponse>> GetAllBudgetsAsync(int skip = 0, int limit = 100)
    {
        var budgets = await _context.Budgets
            .Find(_ => true)
            .SortByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return budgets.Select(MapToResponse).ToList();
    }

    public async Task<List<BudgetResponse>> GetActiveBudgetsAsync(int skip = 0, int limit = 100)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<Budget>.Filter.And(
            Builders<Budget>.Filter.Eq(b => b.IsActive, true),
            Builders<Budget>.Filter.Lte(b => b.StartDate, now),
            Builders<Budget>.Filter.Gte(b => b.EndDate, now)
        );

        var budgets = await _context.Budgets
            .Find(filter)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return budgets.Select(MapToResponse).ToList();
    }

    public async Task<List<BudgetResponse>> GetBudgetsByAccountAsync(string accountId, int skip = 0, int limit = 100)
    {
        var budgets = await _context.Budgets
            .Find(b => b.AccountId == accountId)
            .SortByDescending(b => b.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return budgets.Select(MapToResponse).ToList();
    }

    public async Task<BudgetResponse?> UpdateBudgetAsync(string id, UpdateBudgetRequest request)
    {
        var budget = await _context.Budgets.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (budget == null) return null;

        if (request.Name != null) budget.Name = request.Name;
        if (request.Amount.HasValue)
        {
            budget.Amount = request.Amount.Value;
            budget.Remaining = budget.Amount - budget.Spent;
        }
        if (request.IsActive.HasValue) budget.IsActive = request.IsActive.Value;

        budget.UpdatedAt = DateTime.UtcNow;

        await _context.Budgets.ReplaceOneAsync(b => b.Id == id, budget);

        _logger.LogInformation("Updated budget {Name}", budget.Name);

        return MapToResponse(budget);
    }

    public async Task<bool> DeleteBudgetAsync(string id)
    {
        var budget = await _context.Budgets.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (budget == null) return false;

        // Soft delete
        budget.IsActive = false;
        budget.UpdatedAt = DateTime.UtcNow;

        await _context.Budgets.ReplaceOneAsync(b => b.Id == id, budget);

        _logger.LogInformation("Deactivated budget {Name}", budget.Name);

        return true;
    }

    public async Task UpdateBudgetSpendingAsync(string accountId, decimal amount)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<Budget>.Filter.And(
            Builders<Budget>.Filter.Eq(b => b.AccountId, accountId),
            Builders<Budget>.Filter.Eq(b => b.IsActive, true),
            Builders<Budget>.Filter.Lte(b => b.StartDate, now),
            Builders<Budget>.Filter.Gte(b => b.EndDate, now)
        );

        var budgets = await _context.Budgets.Find(filter).ToListAsync();

        foreach (var budget in budgets)
        {
            budget.Spent += amount;
            budget.Remaining = budget.Amount - budget.Spent;
            budget.UpdatedAt = DateTime.UtcNow;

            await _context.Budgets.ReplaceOneAsync(b => b.Id == budget.Id, budget);

            // Check if budget exceeded
            if (budget.Spent > budget.Amount)
            {
                _logger.LogWarning("Budget {Name} exceeded: {Spent} / {Amount}", 
                    budget.Name, budget.Spent, budget.Amount);

                // Publish budget exceeded event
                var budgetEvent = new BudgetExceededEvent
                {
                    BudgetId = budget.Id,
                    BudgetName = budget.Name,
                    AccountId = budget.AccountId,
                    BudgetAmount = budget.Amount,
                    Spent = budget.Spent,
                    ExceededBy = budget.Spent - budget.Amount,
                    DetectedAt = DateTime.UtcNow
                };

                await _kafkaProducer.PublishAsync(FinancialTopic, budget.Id, budgetEvent);
            }
        }
    }

    private static BudgetResponse MapToResponse(Budget budget)
    {
        var percentageUsed = budget.Amount > 0 ? (budget.Spent / budget.Amount) * 100 : 0;

        return new BudgetResponse
        {
            Id = budget.Id,
            Name = budget.Name,
            AccountId = budget.AccountId,
            Period = budget.Period.ToString(),
            StartDate = budget.StartDate,
            EndDate = budget.EndDate,
            Amount = budget.Amount,
            Spent = budget.Spent,
            Remaining = budget.Remaining,
            PercentageUsed = Math.Round(percentageUsed, 2),
            IsActive = budget.IsActive,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt
        };
    }
}
