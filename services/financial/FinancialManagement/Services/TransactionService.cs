using Microsoft.EntityFrameworkCore;
using MassTransit;
using FinancialManagement.Infrastructure;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;
using ERP.Contracts.Events.Domain;

namespace FinancialManagement.Services;

public interface ITransactionService
{
    Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, string createdBy);
    Task<TransactionResponse?> GetTransactionByIdAsync(string id);
    Task<List<TransactionResponse>> GetAllTransactionsAsync(int skip = 0, int limit = 100);
    Task<List<TransactionResponse>> GetTransactionsByAccountAsync(string accountId, int skip = 0, int limit = 100);
    Task<List<TransactionResponse>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int limit = 100);
    Task<TransactionResponse?> VoidTransactionAsync(string id);
}

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _dbContext;
    private readonly IAccountService _accountService;
    private readonly ITopicProducer<TransactionCreated> _transactionCreatedProducer;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        AppDbContext dbContext,
        IAccountService accountService,
        ITopicProducer<TransactionCreated> transactionCreatedProducer,
        ILogger<TransactionService> logger)
    {
        _dbContext = dbContext;
        _accountService = accountService;
        _transactionCreatedProducer = transactionCreatedProducer;
        _logger = logger;
    }

    public async Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, string createdBy)
    {
        // Validate transaction type
        if (!Enum.TryParse<TransactionType>(request.Type, true, out var transactionType))
        {
            throw new ArgumentException($"Invalid transaction type: {request.Type}");
        }

        // Validate double-entry: total debits must equal total credits
        var totalDebits = request.Entries.Sum(e => e.Debit);
        var totalCredits = request.Entries.Sum(e => e.Credit);

        if (totalDebits != totalCredits)
        {
            throw new InvalidOperationException(
                $"Transaction not balanced. Debits: {totalDebits}, Credits: {totalCredits}");
        }

        // Validate all accounts exist
        var entries = new List<JournalEntry>();
        foreach (var entryRequest in request.Entries)
        {
            // Resolve REVENUE_ACCOUNT placeholder to actual revenue account
            var accountId = entryRequest.AccountId;
            if (accountId == "REVENUE_ACCOUNT")
            {
                accountId = await GetRevenueAccountIdAsync();
                if (string.IsNullOrEmpty(accountId))
                {
                    throw new InvalidOperationException("Revenue account not found. Please create a revenue account first.");
                }
            }

            var account = await _accountService.GetAccountByIdAsync(accountId);
            if (account == null)
            {
                throw new ArgumentException($"Account {accountId} not found");
            }

            entries.Add(new JournalEntry
            {
                AccountId = accountId,
                AccountName = account.Name,
                Debit = entryRequest.Debit,
                Credit = entryRequest.Credit,
                Memo = entryRequest.Memo
            });
        }

        var transactionNumber = await GenerateTransactionNumberAsync();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid().ToString(),
            TransactionNumber = transactionNumber,
            Date = request.Date.HasValue 
                ? DateTime.SpecifyKind(request.Date.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow,
            Description = request.Description,
            Entries = entries,
            Type = transactionType,
            Status = TransactionStatus.Posted,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Use EF Core transaction for atomicity
        using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Insert transaction
            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            // Update account balances
            foreach (var entry in entries)
            {
                await UpdateAccountBalanceAsync(entry.AccountId, entry.Debit, entry.Credit);
            }

            await dbTransaction.CommitAsync();

            _logger.LogInformation("Created transaction {TransactionNumber}: {Description}",
                transactionNumber, request.Description);

            // Fire-and-forget: dashboard domain event, not saga-critical. Decoupled from the hot path.
            _ = _transactionCreatedProducer.Produce(new TransactionCreated
            {
                TransactionId = transaction.Id,
                Description = transaction.Description,
                Type = transaction.Type.ToString(),
                TotalAmount = totalDebits
            }).ContinueWith(
                t => _logger.LogError(t.Exception, "Failed to publish TransactionCreated to Kafka"),
                TaskContinuationOptions.OnlyOnFaulted);

            return MapToResponse(transaction);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create transaction");
            throw;
        }
    }

    public async Task<TransactionResponse?> GetTransactionByIdAsync(string id)
    {
        var transaction = await _dbContext.Transactions.FindAsync(id);
        return transaction != null ? MapToResponse(transaction) : null;
    }

    public async Task<List<TransactionResponse>> GetAllTransactionsAsync(int skip = 0, int limit = 100)
    {
        var transactions = await _dbContext.Transactions
            .Where(t => t.Status == TransactionStatus.Posted)
            .OrderByDescending(t => t.Date)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        return transactions.Select(MapToResponse).ToList();
    }

    public async Task<List<TransactionResponse>> GetTransactionsByAccountAsync(string accountId, int skip = 0, int limit = 100)
    {
        var transactions = await _dbContext.Transactions
            .Where(t => t.Status == TransactionStatus.Posted &&
                       t.Entries.Any(e => e.AccountId == accountId))
            .OrderByDescending(t => t.Date)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        return transactions.Select(MapToResponse).ToList();
    }

    public async Task<List<TransactionResponse>> GetTransactionsByDateRangeAsync(
        DateTime startDate, DateTime endDate, int skip = 0, int limit = 100)
    {
        var transactions = await _dbContext.Transactions
            .Where(t => t.Status == TransactionStatus.Posted &&
                       t.Date >= startDate &&
                       t.Date <= endDate)
            .OrderByDescending(t => t.Date)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        return transactions.Select(MapToResponse).ToList();
    }

    public async Task<TransactionResponse?> VoidTransactionAsync(string id)
    {
        var transaction = await _dbContext.Transactions.FindAsync(id);
        if (transaction == null) return null;

        if (transaction.Status == TransactionStatus.Voided)
        {
            throw new InvalidOperationException("Transaction is already voided");
        }

        using var dbTransaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Reverse account balance changes
            foreach (var entry in transaction.Entries)
            {
                // Reverse: debits become credits, credits become debits
                await UpdateAccountBalanceAsync(entry.AccountId, entry.Credit, entry.Debit);
            }

            // Update transaction status
            transaction.Status = TransactionStatus.Voided;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            _logger.LogInformation("Voided transaction {TransactionNumber}", transaction.TransactionNumber);

            return MapToResponse(transaction);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Failed to void transaction {TransactionNumber}", transaction.TransactionNumber);
            throw;
        }
    }

    private async Task UpdateAccountBalanceAsync(string accountId, decimal debit, decimal credit)
    {
        var account = await _dbContext.Accounts.FindAsync(accountId);

        if (account == null)
        {
            throw new InvalidOperationException($"Account {accountId} not found");
        }

        // Update balance based on account type
        // Assets and Expenses increase with debits
        // Liabilities, Equity, and Revenue increase with credits
        var balanceChange = account.Type switch
        {
            AccountType.Asset => debit - credit,
            AccountType.Expense => debit - credit,
            AccountType.Liability => credit - debit,
            AccountType.Equity => credit - debit,
            AccountType.Revenue => credit - debit,
            _ => 0
        };

        account.Balance += balanceChange;
        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
    }

    private async Task<string> GenerateTransactionNumberAsync()
    {
        var count = await _dbContext.Transactions.CountAsync();
        return $"TXN-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D6}";
    }

    private async Task<string?> GetRevenueAccountIdAsync()
    {
        // Look up revenue account by name and type
        var revenueAccount = await _dbContext.Accounts
            .FirstOrDefaultAsync(a =>
                a.Name == "Product Sales Revenue" &&
                a.Type == AccountType.Asset &&
                a.Category == AccountCategory.CurrentAssets &&
                a.IsActive);

        return revenueAccount?.Id;
    }

    private static TransactionResponse MapToResponse(Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            TransactionNumber = transaction.TransactionNumber,
            Date = transaction.Date,
            Description = transaction.Description,
            Entries = transaction.Entries,
            Type = transaction.Type.ToString(),
            Status = transaction.Status.ToString(),
            ReferenceId = transaction.ReferenceId,
            ReferenceType = transaction.ReferenceType,
            CreatedBy = transaction.CreatedBy,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
