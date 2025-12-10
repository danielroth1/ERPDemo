using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinancialManagement.Models;

public class Account
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("accountNumber")]
    public string AccountNumber { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public AccountType Type { get; set; }

    [BsonElement("category")]
    public AccountCategory Category { get; set; }

    [BsonElement("balance")]
    public decimal Balance { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "USD";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("parentAccountId")]
    public string? ParentAccountId { get; set; }

    [BsonElement("userId")]
    public string? UserId { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Transaction
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("transactionNumber")]
    public string TransactionNumber { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("entries")]
    public List<JournalEntry> Entries { get; set; } = new();

    [BsonElement("type")]
    public TransactionType Type { get; set; }

    [BsonElement("status")]
    public TransactionStatus Status { get; set; } = TransactionStatus.Posted;

    [BsonElement("referenceId")]
    public string? ReferenceId { get; set; }

    [BsonElement("referenceType")]
    public string? ReferenceType { get; set; }

    [BsonElement("attachments")]
    public List<string> Attachments { get; set; } = new();

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class JournalEntry
{
    [BsonElement("accountId")]
    public string AccountId { get; set; } = string.Empty;

    [BsonElement("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [BsonElement("debit")]
    public decimal Debit { get; set; }

    [BsonElement("credit")]
    public decimal Credit { get; set; }

    [BsonElement("memo")]
    public string? Memo { get; set; }
}

public class Budget
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("accountId")]
    public string AccountId { get; set; } = string.Empty;

    [BsonElement("period")]
    public BudgetPeriod Period { get; set; }

    [BsonElement("startDate")]
    public DateTime StartDate { get; set; }

    [BsonElement("endDate")]
    public DateTime EndDate { get; set; }

    [BsonElement("amount")]
    public decimal Amount { get; set; }

    [BsonElement("spent")]
    public decimal Spent { get; set; }

    [BsonElement("remaining")]
    public decimal Remaining { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

public enum AccountCategory
{
    // Assets
    CurrentAssets,
    FixedAssets,
    OtherAssets,
    
    // Liabilities
    CurrentLiabilities,
    LongTermLiabilities,
    
    // Equity
    OwnersEquity,
    RetainedEarnings,
    
    // Revenue
    OperatingRevenue,
    NonOperatingRevenue,
    
    // Expenses
    CostOfGoodsSold,
    OperatingExpenses,
    NonOperatingExpenses
}

public enum TransactionType
{
    Sale,
    Return,
    Purchase,
    Payment,
    Receipt,
    Expense,
    Adjustment,
    Transfer,
    Opening,
    Closing
}

public enum TransactionStatus
{
    Draft,
    Posted,
    Voided
}

public enum BudgetPeriod
{
    Monthly,
    Quarterly,
    Yearly
}
