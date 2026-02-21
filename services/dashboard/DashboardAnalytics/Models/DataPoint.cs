namespace DashboardAnalytics.Models;

public class DataPoint
{
    public string Label { get; set; } = string.Empty;
    
    public decimal Value { get; set; }
    
    public string? Category { get; set; }
    
    public DateTime? Timestamp { get; set; }
}
