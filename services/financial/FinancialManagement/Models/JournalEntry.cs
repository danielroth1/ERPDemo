namespace FinancialManagement.Models;

public class JournalEntry
{
    public string AccountId { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public string? Memo { get; set; }
}
