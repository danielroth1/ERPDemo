using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DashboardAnalytics.Models;

public class DashboardMetrics
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public MetricType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class KPI
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal PercentageChange => PreviousValue != 0 
        ? ((CurrentValue - PreviousValue) / PreviousValue) * 100 
        : 0;
    public KPIStatus Status { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class ChartData
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string ChartId { get; set; } = string.Empty;
    public ChartType Type { get; set; }
    public string Title { get; set; } = string.Empty;
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

public class Alert
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Source { get; set; } = string.Empty;
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
