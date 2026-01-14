using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FinancialManagement.Models;

[Table("accounts")]
[Index(nameof(AccountNumber), IsUnique = true)]
[Index(nameof(Type))]
[Index(nameof(UserId))]
public class Account
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(50)]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public AccountCategory Category { get; set; }

    [Precision(18, 2)]
    public decimal Balance { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public bool IsActive { get; set; } = true;

    public string? ParentAccountId { get; set; }

    public string? UserId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("transactions")]
[Index(nameof(TransactionNumber), IsUnique = true)]
[Index(nameof(Date))]
[Index(nameof(Type))]
[Index(nameof(Status))]
public class Transaction
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(50)]
    public string TransactionNumber { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public List<JournalEntry> Entries { get; set; } = new();

    public TransactionType Type { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Posted;

    public string? ReferenceId { get; set; }

    [MaxLength(50)]
    public string? ReferenceType { get; set; }

    [Column(TypeName = "jsonb")]
    public List<string> Attachments { get; set; } = new();

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class JournalEntry
{
    public string AccountId { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public string? Memo { get; set; }
}

[Table("budgets")]
[Index(nameof(AccountId))]
public class Budget
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string AccountId { get; set; } = string.Empty;

    public BudgetPeriod Period { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    [Precision(18, 2)]
    public decimal Amount { get; set; }

    [Precision(18, 2)]
    public decimal Spent { get; set; }

    [Precision(18, 2)]
    public decimal Remaining { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

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
