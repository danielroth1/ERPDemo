using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DashboardAnalytics.Models;

[Table("chart_data")]
[Index(nameof(ChartId))]
public class ChartData
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(50)]
    public string ChartId { get; set; } = string.Empty;
    
    public ChartType Type { get; set; }
    
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public List<DataPoint> DataPoints { get; set; } = new();
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
