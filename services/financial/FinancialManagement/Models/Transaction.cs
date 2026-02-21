using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FinancialManagement.Models;

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
