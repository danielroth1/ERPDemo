using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DashboardAnalytics.Models;

[Table("kpis")]
[Index(nameof(Name), IsUnique = true)]
public class KPI
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Precision(18, 2)]
    public decimal CurrentValue { get; set; }
    
    [Precision(18, 2)]
    public decimal TargetValue { get; set; }
    
    [Precision(18, 2)]
    public decimal PreviousValue { get; set; }
    
    [NotMapped]
    public decimal PercentageChange => PreviousValue != 0 
        ? ((CurrentValue - PreviousValue) / PreviousValue) * 100 
        : 0;
    
    public KPIStatus Status { get; set; }
    
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
