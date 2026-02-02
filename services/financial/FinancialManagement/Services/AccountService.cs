using Microsoft.EntityFrameworkCore;
using FinancialManagement.Infrastructure;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;

namespace FinancialManagement.Services;

public interface IAccountService
{
    Task<AccountResponse> CreateAccountAsync(CreateAccountRequest request);
    Task<(AccountResponse AssetAccount, AccountResponse ExpenseAccount)> CreateUserAccountsAsync(string userId, string userName);
    Task<AccountResponse?> GetAccountByIdAsync(string id);
    Task<AccountResponse?> GetAccountByNumberAsync(string accountNumber);
    Task<AccountResponse?> GetAccountByUserIdAsync(string userId);
    Task<AccountResponse?> GetAccountByUserIdAndTypeAsync(string userId, AccountType type);
    Task<AccountResponse?> GetAccountByNameAsync(string name);
    Task<List<AccountResponse>> GetAllAccountsAsync(int skip = 0, int limit = 100);
    Task<List<AccountResponse>> GetAccountsByTypeAsync(AccountType type, int skip = 0, int limit = 100);
    Task<AccountResponse?> UpdateAccountAsync(string id, UpdateAccountRequest request);
    Task<bool> DeleteAccountAsync(string id);
    Task<decimal> GetAccountBalanceAsync(string id);
    Task<AccountResponse?> AdjustBalanceAsync(string id, decimal amount);
    Task<List<Account>> GetSystemAccountsAsync();
    Task<AccountBalanceSummary> GetAccountBalanceSummaryAsync();
}

public class AccountService : IAccountService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AccountService> _logger;

    public AccountService(AppDbContext dbContext, ILogger<AccountService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(AccountResponse AssetAccount, AccountResponse ExpenseAccount)> CreateUserAccountsAsync(string userId, string userName)
    {
        // Check if user already has accounts
        var existingAssetAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Type == AccountType.Asset && a.IsActive);
        var existingExpenseAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Type == AccountType.Expense && a.IsActive);

        if (existingAssetAccount != null || existingExpenseAccount != null)
        {
            throw new ArgumentException($"User already has accounts. Asset: {existingAssetAccount?.AccountNumber}, Expense: {existingExpenseAccount?.AccountNumber}");
        }

        // Create Asset account
        var assetAccount = await CreateAccountAsync(new CreateAccountRequest
        {
            Name = $"{userName} - Personal Account",
            Type = "Asset",
            Category = "CurrentAssets",
            Currency = "USD",
            UserId = userId,
            Description = $"Personal asset account for user {userId}"
        });

        // Create Expense account
        var expenseAccount = await CreateAccountAsync(new CreateAccountRequest
        {
            Name = $"{userName} - Expense Account",
            Type = "Expense",
            Category = "OperatingExpenses",
            Currency = "USD",
            UserId = userId,
            Description = $"Expense account for user {userId}"
        });

        _logger.LogInformation("Created user accounts for {UserId}: Asset={AssetAccountNumber}, Expense={ExpenseAccountNumber}",
            userId, assetAccount.AccountNumber, expenseAccount.AccountNumber);

        return (assetAccount, expenseAccount);
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

        // Check if user already has an account of the same type
        // Users can have multiple accounts (Asset, Expense), but only one of each type
        if (!string.IsNullOrEmpty(request.UserId))
        {
            var existingAccount = await _dbContext.Accounts
                .FirstOrDefaultAsync(a => a.UserId == request.UserId && a.Type == accountType && a.IsActive);

            if (existingAccount != null)
            {
                throw new ArgumentException($"User already has a {accountType} account: {existingAccount.AccountNumber}");
            }
        }

        var accountNumber = await GenerateAccountNumberAsync(accountType);

        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            AccountNumber = accountNumber,
            Name = request.Name,
            Type = accountType,
            Category = category,
            Currency = request.Currency,
            ParentAccountId = request.ParentAccountId,
            UserId = request.UserId,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created account {AccountNumber} - {Name} for user {UserId}",
            accountNumber, request.Name, request.UserId ?? "(system)");

        return MapToResponse(account);
    }

    public async Task<AccountResponse?> GetAccountByIdAsync(string id)
    {
        var account = await _dbContext.Accounts.FindAsync(id);
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<AccountResponse?> GetAccountByNumberAsync(string accountNumber)
    {
        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<AccountResponse?> GetAccountByUserIdAsync(string userId)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Type == AccountType.Asset && a.IsActive);
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<AccountResponse?> GetAccountByUserIdAndTypeAsync(string userId, AccountType type)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Type == type && a.IsActive);
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<AccountResponse?> GetAccountByNameAsync(string name)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Name == name && a.IsActive);
        return account != null ? MapToResponse(account) : null;
    }

    public async Task<List<AccountResponse>> GetAllAccountsAsync(int skip = 0, int limit = 100)
    {
        var accounts = await _dbContext.Accounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.AccountNumber)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        return accounts.Select(MapToResponse).ToList();
    }

    public async Task<List<AccountResponse>> GetAccountsByTypeAsync(AccountType type, int skip = 0, int limit = 100)
    {
        var accounts = await _dbContext.Accounts
            .Where(a => a.Type == type && a.IsActive)
            .OrderBy(a => a.AccountNumber)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        return accounts.Select(MapToResponse).ToList();
    }

    public async Task<AccountResponse?> UpdateAccountAsync(string id, UpdateAccountRequest request)
    {
        var account = await _dbContext.Accounts.FindAsync(id);
        if (account == null) return null;

        if (request.Name != null) account.Name = request.Name;
        if (request.Description != null) account.Description = request.Description;
        if (request.IsActive.HasValue) account.IsActive = request.IsActive.Value;

        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated account {AccountNumber}", account.AccountNumber);

        return MapToResponse(account);
    }

    public async Task<bool> DeleteAccountAsync(string id)
    {
        var account = await _dbContext.Accounts.FindAsync(id);
        if (account == null) return false;

        // Check if account has balance
        if (account.Balance != 0)
        {
            throw new InvalidOperationException($"Cannot delete account with non-zero balance: {account.Balance}");
        }

        // Soft delete
        account.IsActive = false;
        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deactivated account {AccountNumber}", account.AccountNumber);

        return true;
    }

    public async Task<decimal> GetAccountBalanceAsync(string id)
    {
        var account = await _dbContext.Accounts.FindAsync(id);
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

        var count = await _dbContext.Accounts.CountAsync(a => a.Type == type);
        return $"{prefix}{(count + 1):D4}";
    }

    public async Task<AccountResponse?> AdjustBalanceAsync(string id, decimal amount)
    {
        var account = await _dbContext.Accounts.FindAsync(id);
        if (account == null) return null;

        account.Balance += amount;
        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Adjusted account {AccountNumber} balance by {Amount}. New balance: {Balance}",
            account.AccountNumber, amount, account.Balance);

        return MapToResponse(account);
    }

    public async Task<List<Account>> GetSystemAccountsAsync()
    {
        var systemAccountNames = new[]
        {
            "Company Operating Account",
            "Sales Tax Payable",
            "Product Inventory",
            "Cost of Goods Sold",
            "Product Sales Revenue"
        };

        var systemAccounts = await _dbContext.Accounts
            .Where(a => systemAccountNames.Contains(a.Name) && a.IsActive)
            .ToListAsync();

        return systemAccounts;
    }

    public async Task<AccountBalanceSummary> GetAccountBalanceSummaryAsync()
    {
        var systemAccounts = await GetSystemAccountsAsync();

        var totalAssets = systemAccounts
            .Where(a => a.Type == AccountType.Asset)
            .Sum(a => a.Balance);

        var totalLiabilities = systemAccounts
            .Where(a => a.Type == AccountType.Liability)
            .Sum(a => a.Balance);

        var totalEquity = systemAccounts
            .Where(a => a.Type == AccountType.Equity)
            .Sum(a => a.Balance);

        return new AccountBalanceSummary
        {
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity
        };
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
