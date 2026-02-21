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
