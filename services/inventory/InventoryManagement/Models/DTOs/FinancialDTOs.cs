namespace InventoryManagement.Models.DTOs;

/// <summary>
/// Request to create a financial transaction
/// </summary>
public class CreateFinancialTransactionRequest
{
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public List<FinancialJournalEntry> Entries { get; set; } = new();
}

/// <summary>
/// Journal entry for a financial transaction
/// </summary>
public class FinancialJournalEntry
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
}
