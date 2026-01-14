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

public class DataPoint
{
    public string Label { get; set; } = string.Empty;
    
    public decimal Value { get; set; }
    
    public string? Category { get; set; }
    
    public DateTime? Timestamp { get; set; }
}

[Table("alerts")]
[Index(nameof(Severity))]
[Index(nameof(IsRead))]
[Index(nameof(CreatedAt))]
public class Alert
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public AlertSeverity Severity { get; set; }
    
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> Data { get; set; } = new();
    
    public bool IsRead { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MetricType
{
    UserCount,
    ProductCount,
    OrderCount,
    Revenue,
    Expenses,
    NetIncome,
    InventoryValue,
    LowStockProducts,
    OrdersToday,
    CustomerCount
}

public enum KPIStatus
{
    OnTrack,
    NeedsAttention,
    Critical
}

public enum ChartType
{
    Line,
    Bar,
    Pie,
    Area,
    Doughnut
}

public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
