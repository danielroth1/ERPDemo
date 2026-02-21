namespace DashboardAnalytics.Models;

public class TableInfo
{
    public string Name { get; set; } = string.Empty;
    
    public long RowCount { get; set; }
    
    public long SizeInBytes { get; set; }
    
    public double AverageSizeInBytes { get; set; }
    
    public List<IndexInfo> Indexes { get; set; } = new();
    
    public string? SampleDocument { get; set; }
    
    public Dictionary<string, string> Schema { get; set; } = new();
}
