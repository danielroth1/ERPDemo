using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FinancialManagement.Models;

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
