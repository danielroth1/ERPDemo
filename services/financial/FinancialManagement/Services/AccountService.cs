using MongoDB.Driver;
using FinancialManagement.Infrastructure;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;

namespace FinancialManagement.Services;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request);
    Task<AccountResponse?> GetAccountByIdAsync(string id);
    Task<AccountResponse?> GetAccountByNumberAsync(string accountNumber);
    Task<AccountResponse?> GetAccountByUserIdAsync(string userId);
    Task<List<AccountResponse>> GetAllAccountsAsync(int skip = 0, int limit = 100);
    Task<List<AccountResponse>> GetAccountsByTypeAsync(AccountType type, int skip = 0, int limit = 100);
    Task<AccountResponse?> UpdateAccountAsync(string id, UpdateAccountRequest request);
    Task<bool> DeleteAccountAsync(string id);
    Task<decimal> GetAccountBalanceAsync(string id);
    Task<AccountResponse?> AdjustBalanceAsync(string id, decimal amount);
}

public class AccountService : IAccountService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<AccountService> _logger;

    public AccountService(MongoDbContext context, ILogger<AccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request)
    {
        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
        {
            var validTypes = string.Join(", ", Enum.GetNames<AccountType>());
            throw new ArgumentException($"Invalid account type: '{request.Type}'. Valid types are: {validTypes}");
        }

        if (!Enum.TryParse<AccountCategory>(request.Category, true, out var category))
        {
            var validCategories = string.Join(", ", Enum.GetNames<AccountCategory>());
            throw new ArgumentException($"Invalid account category: '{request.Category}'. Valid categories are: {validCategories}");
        }

        // Check if user already has an account (1:1 relationship)
        if (!string.IsNullOrEmpty(request.UserId))
        {
            var existingAccount = await _context.Accounts
                .Find(a => a.UserId == request.UserId && a.IsActive)
                .FirstOrDefaultAsync();
            
            if (existingAccount != null)
            {
                throw new ArgumentException($"User already has an account: {existingAccount.AccountNumber}");
            }
        }

        var accountNumber = await GenerateAccountNumberAsync(accountType);

        var account = new Account
        {
            AccountNumber = accountNumber,
            Name = request.Name,
            Type = accountType,
            Category = category,
            Currency = request.Currency,
            ParentAccountId = request.ParentAccountId,
            UserId = request.UserId,
            Description = request.Description
        };

        await _context.Accounts.InsertOneAsync(account);

        _logger.LogInformation("Created account {AccountNumber} - {Name} for user {UserId}", 
            accountNumber, request.Name, request.UserId ?? "(system)");

        return MapToResponse(account);
    }

    public async Task<AccountResponse?> GetAccountByIdAsync(string id)
    {
        var account = await _context.Accounts.Find(a => a.Id == id).FirstOrDefaultAsync();
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<AccountResponse?> GetAccountByNumberAsync(string accountNumber)
    {
        var account = await _context.Accounts.Find(a => a.AccountNumber == accountNumber).FirstOrDefaultAsync();
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<AccountResponse?> GetAccountByUserIdAsync(string userId)
    {
        var account = await _context.Accounts
            .Find(a => a.UserId == userId && a.IsActive)
            .FirstOrDefaultAsync();
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<List<AccountResponse>> GetAllAccountsAsync(int skip = 0, int limit = 100)
    {
        var accounts = await _context.Accounts
            .Find(a => a.IsActive)
            .SortBy(a => a.AccountNumber)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return accounts.Select(MapToResponse).ToList();
    }

    public async Task<List<AccountResponse>> GetAccountsByTypeAsync(AccountType type, int skip = 0, int limit = 100)
    {
        var accounts = await _context.Accounts
            .Find(a => a.Type == type && a.IsActive)
            .SortBy(a => a.AccountNumber)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return accounts.Select(MapToResponse).ToList();
    }

    public async Task<AccountResponse?> UpdateAccountAsync(string id, UpdateAccountRequest request)
    {
        var account = await _context.Accounts.Find(a => a.Id == id).FirstOrDefaultAsync();
        if (account == null) return null;

        if (request.Name != null) account.Name = request.Name;
        if (request.Description != null) account.Description = request.Description;
        if (request.IsActive.HasValue) account.IsActive = request.IsActive.Value;

        account.UpdatedAt = DateTime.UtcNow;

        await _context.Accounts.ReplaceOneAsync(a => a.Id == id, account);

        _logger.LogInformation("Updated account {AccountNumber}", account.AccountNumber);

        return MapToResponse(account);
    }

    public async Task<bool> DeleteAccountAsync(string id)
    {
        var account = await _context.Accounts.Find(a => a.Id == id).FirstOrDefaultAsync();
        if (account == null) return false;

        // Check if account has balance
        if (account.Balance != 0)
        {
            throw new InvalidOperationException($"Cannot delete account with non-zero balance: {account.Balance}");
        }

        // Soft delete
        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.Accounts.ReplaceOneAsync(a => a.Id == id, account);

        _logger.LogInformation("Deactivated account {AccountNumber}", account.AccountNumber);

        return true;
    }

    public async Task<decimal> GetAccountBalanceAsync(string id)
    {
        var account = await _context.Accounts.Find(a => a.Id == id).FirstOrDefaultAsync();
        return account?.Balance ?? 0;
    }

    private async Task<string> GenerateAccountNumberAsync(AccountType type)
    {
        var prefix = type switch
        {
            AccountType.Asset => "1",
            AccountType.Liability => "2",
            AccountType.Equity => "3",
            AccountType.Revenue => "4",
            AccountType.Expense => "5",
            _ => "9"
        };

        var count = await _context.Accounts.CountDocumentsAsync(a => a.Type == type);
        return $"{prefix}{(count + 1):D4}";
    }

    public async Task<AccountResponse?> AdjustBalanceAsync(string id, decimal amount)
    {
        var account = await _context.Accounts.Find(a => a.Id == id).FirstOrDefaultAsync();
        if (account == null) return null;

        account.Balance += amount;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.Accounts.ReplaceOneAsync(a => a.Id == id, account);

        _logger.LogInformation("Adjusted account {AccountNumber} balance by {Amount}. New balance: {Balance}",
            account.AccountNumber, amount, account.Balance);

        return MapToResponse(account);
    }

    private static AccountResponse MapToResponse(Account account)
    {
        return new AccountResponse
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            Name = account.Name,
            Type = account.Type.ToString(),
            Category = account.Category.ToString(),
            Balance = account.Balance,
            Currency = account.Currency,
            IsActive = account.IsActive,
            ParentAccountId = account.ParentAccountId,
            UserId = account.UserId,
            Description = account.Description,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}
