using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DashboardAnalytics.Models;

[Table("dashboard_metrics")]
[Index(nameof(Timestamp))]
[Index(nameof(Type))]
public class DashboardMetrics
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public MetricType Type { get; set; }
    
    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;
    
    [Precision(18, 2)]
    public decimal Value { get; set; }
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> Metadata { get; set; } = new();
}
