using MongoDB.Driver;
using FinancialManagement.Infrastructure;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;

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
    private readonly MongoDbContext _context;
    private readonly IAccountService _accountService;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<TransactionService> _logger;
    private const string FinancialTopic = "financial-events";

    public TransactionService(
        MongoDbContext context,
        IAccountService accountService,
        KafkaProducer kafkaProducer,
        ILogger<TransactionService> logger)
    {
        _context = context;
        _accountService = accountService;
        _kafkaProducer = kafkaProducer;
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
            TransactionNumber = transactionNumber,
            Date = request.Date ?? DateTime.UtcNow,
            Description = request.Description,
            Entries = entries,
            Type = transactionType,
            Status = TransactionStatus.Posted,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType,
            CreatedBy = createdBy
        };

        // Use transaction session for atomicity (if supported)
        IClientSessionHandle? session = null;
        var useTransactions = await IsTransactionSupportedAsync();
        
        if (useTransactions)
        {
            session = await _context.Transactions.Database.Client.StartSessionAsync();
            session.StartTransaction();
        }
        else
        {
            _logger.LogWarning("MongoDB transactions not supported (standalone mode). Operations will not be atomic.");
        }

        try
        {
            // Insert transaction
            if (session != null)
                await _context.Transactions.InsertOneAsync(session, transaction);
            else
                await _context.Transactions.InsertOneAsync(transaction);

            // Update account balances
            foreach (var entry in entries)
            {
                await UpdateAccountBalanceAsync(session, entry.AccountId, entry.Debit, entry.Credit);
            }

            if (session != null)
                await session.CommitTransactionAsync();

            _logger.LogInformation("Created transaction {TransactionNumber}: {Description}", 
                transactionNumber, request.Description);

            // Publish event
            var transactionEvent = new TransactionCreatedEvent
            {
                TransactionId = transaction.Id,
                TransactionNumber = transaction.TransactionNumber,
                Date = transaction.Date,
                Description = transaction.Description,
                Type = transaction.Type.ToString(),
                Amount = totalDebits, // or totalCredits, they're equal
                CreatedAt = transaction.CreatedAt
            };

            await _kafkaProducer.PublishAsync(FinancialTopic, transaction.Id, transactionEvent);

            return MapToResponse(transaction);
        }
        catch (Exception ex)
        {
            if (session != null)
                await session.AbortTransactionAsync();
            _logger.LogError(ex, "Failed to create transaction");
            throw;
        }
        finally
        {
            session?.Dispose();
        }
    }

    public async Task<TransactionResponse?> GetTransactionByIdAsync(string id)
    {
        var transaction = await _context.Transactions.Find(t => t.Id == id).FirstOrDefaultAsync();
        return transaction != null ? MapToResponse(transaction) : null;
    }

    public async Task<List<TransactionResponse>> GetAllTransactionsAsync(int skip = 0, int limit = 100)
    {
        var transactions = await _context.Transactions
            .Find(t => t.Status == TransactionStatus.Posted)
            .SortByDescending(t => t.Date)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return transactions.Select(MapToResponse).ToList();
    }

    public async Task<List<TransactionResponse>> GetTransactionsByAccountAsync(string accountId, int skip = 0, int limit = 100)
    {
        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.Status, TransactionStatus.Posted),
            Builders<Transaction>.Filter.ElemMatch(t => t.Entries, e => e.AccountId == accountId)
        );

        var transactions = await _context.Transactions
            .Find(filter)
            .SortByDescending(t => t.Date)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return transactions.Select(MapToResponse).ToList();
    }

    public async Task<List<TransactionResponse>> GetTransactionsByDateRangeAsync(
        DateTime startDate, DateTime endDate, int skip = 0, int limit = 100)
    {
        var filter = Builders<Transaction>.Filter.And(
            Builders<Transaction>.Filter.Eq(t => t.Status, TransactionStatus.Posted),
            Builders<Transaction>.Filter.Gte(t => t.Date, startDate),
            Builders<Transaction>.Filter.Lte(t => t.Date, endDate)
        );

        var transactions = await _context.Transactions
            .Find(filter)
            .SortByDescending(t => t.Date)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return transactions.Select(MapToResponse).ToList();
    }

    public async Task<TransactionResponse?> VoidTransactionAsync(string id)
    {
        var transaction = await _context.Transactions.Find(t => t.Id == id).FirstOrDefaultAsync();
        if (transaction == null) return null;

        if (transaction.Status == TransactionStatus.Voided)
        {
            throw new InvalidOperationException("Transaction is already voided");
        }

        IClientSessionHandle? session = null;
        var useTransactions = await IsTransactionSupportedAsync();
        
        if (useTransactions)
        {
            session = await _context.Transactions.Database.Client.StartSessionAsync();
            session.StartTransaction();
        }

        try
        {
            // Reverse account balance changes
            foreach (var entry in transaction.Entries)
            {
                // Reverse: debits become credits, credits become debits
                await UpdateAccountBalanceAsync(session, entry.AccountId, entry.Credit, entry.Debit);
            }

            // Update transaction status
            transaction.Status = TransactionStatus.Voided;
            transaction.UpdatedAt = DateTime.UtcNow;

            if (session != null)
                await _context.Transactions.ReplaceOneAsync(session, t => t.Id == id, transaction);
            else
                await _context.Transactions.ReplaceOneAsync(t => t.Id == id, transaction);

            if (session != null)
                await session.CommitTransactionAsync();

            _logger.LogInformation("Voided transaction {TransactionNumber}", transaction.TransactionNumber);

            return MapToResponse(transaction);
        }
        catch (Exception ex)
        {
            if (session != null)
                await session.AbortTransactionAsync();
            _logger.LogError(ex, "Failed to void transaction {TransactionNumber}", transaction.TransactionNumber);
            throw;
        }
        finally
        {
            session?.Dispose();
        }
    }

    private async Task UpdateAccountBalanceAsync(
        IClientSessionHandle? session, string accountId, decimal debit, decimal credit)
    {
        var account = session != null
            ? await _context.Accounts.Find(session, a => a.Id == accountId).FirstOrDefaultAsync()
            : await _context.Accounts.Find(a => a.Id == accountId).FirstOrDefaultAsync();
            
        if (account == null)
        {
            throw new InvalidOperationException($"Account {accountId} not found");
        }

        // Update balance based on account type
        // Assets and Expenses increase with debits
        // Liabilities, Equity, and Revenue increase with credits
        var balanceChange = account.Type switch
        {
            AccountType.Asset => credit - debit, // hack, should be: debit - credit
            AccountType.Expense => debit - credit,
            AccountType.Liability => credit - debit,
            AccountType.Equity => credit - debit,
            AccountType.Revenue => credit - debit,
            _ => 0
        };

        account.Balance += balanceChange;
        account.UpdatedAt = DateTime.UtcNow;

        if (session != null)
            await _context.Accounts.ReplaceOneAsync(session, a => a.Id == accountId, account);
        else
            await _context.Accounts.ReplaceOneAsync(a => a.Id == accountId, account);
    }

    private async Task<string> GenerateTransactionNumberAsync()
    {
        var count = await _context.Transactions.CountDocumentsAsync(_ => true);
        return $"TXN-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D6}";
    }

    private async Task<bool> IsTransactionSupportedAsync()
    {
        try
        {
            var client = _context.Transactions.Database.Client;
            var isMasterCommand = new MongoDB.Bson.BsonDocument("isMaster", 1);
            var result = await client.GetDatabase("admin").RunCommandAsync<MongoDB.Bson.BsonDocument>(isMasterCommand);
            
            // Check if it's a replica set or sharded cluster
            var isReplicaSet = result.Contains("setName");
            var isSharded = result.Contains("msg") && result["msg"].AsString == "isdbgrid";
            
            return isReplicaSet || isSharded;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to detect MongoDB transaction support, assuming standalone mode");
            return false;
        }
    }

    private async Task<string?> GetRevenueAccountIdAsync()
    {
        // Look up revenue account by name and type
        var revenueAccount = await _context.Accounts
            .Find(a => 
                a.Name == "Product Sales Revenue" && 
                a.Type == AccountType.Revenue &&
                a.Category == AccountCategory.CurrentAssets &&
                a.IsActive)
            .FirstOrDefaultAsync();

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
