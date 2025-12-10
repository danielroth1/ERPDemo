namespace FinancialManagement.Models.DTOs;

// Account DTOs
public class CreateAccountRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public string? ParentAccountId { get; set; }
    public string? UserId { get; set; }
    public string? Description { get; set; }
}

public class UpdateAccountRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class AccountResponse
{
    public string Id { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? ParentAccountId { get; set; }
    public string? UserId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Transaction DTOs
public class CreateTransactionRequest
{
    public DateTime? Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<JournalEntryRequest> Entries { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
}

public class JournalEntryRequest
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
}

public class TransactionResponse
{
    public string Id { get; set; } = string.Empty;
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<JournalEntry> Entries { get; set; } = new();
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Budget DTOs
public class CreateBudgetRequest
{
    public string Name { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
}

public class UpdateBudgetRequest
{
    public string? Name { get; set; }
    public decimal? Amount { get; set; }
    public bool? IsActive { get; set; }
}

public class BudgetResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public decimal PercentageUsed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Report DTOs
public class BalanceSheetResponse
{
    public DateTime AsOfDate { get; set; }
    public AssetSection Assets { get; set; } = new();
    public LiabilitySection Liabilities { get; set; } = new();
    public EquitySection Equity { get; set; } = new();
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
}

public class AssetSection
{
    public List<AccountBalance> CurrentAssets { get; set; } = new();
    public List<AccountBalance> FixedAssets { get; set; } = new();
    public List<AccountBalance> OtherAssets { get; set; } = new();
    public decimal Total { get; set; }
}

public class LiabilitySection
{
    public List<AccountBalance> CurrentLiabilities { get; set; } = new();
    public List<AccountBalance> LongTermLiabilities { get; set; } = new();
    public decimal Total { get; set; }
}

public class EquitySection
{
    public List<AccountBalance> EquityAccounts { get; set; } = new();
    public decimal Total { get; set; }
}

public class AccountBalance
{
    public string AccountId { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class IncomeStatementResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<AccountBalance> Revenue { get; set; } = new();
    public List<AccountBalance> Expenses { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetIncome { get; set; }
}

// Kafka Events
public class TransactionCreatedEvent
{
    public string TransactionId { get; set; } = string.Empty;
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BudgetExceededEvent
{
    public string BudgetId { get; set; } = string.Empty;
    public string BudgetName { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal Spent { get; set; }
    public decimal ExceededBy { get; set; }
    public DateTime DetectedAt { get; set; }
}
