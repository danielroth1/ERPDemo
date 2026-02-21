namespace DashboardAnalytics.Models;

public class IndexInfo
{
    public string Name { get; set; } = string.Empty;
    
    public Dictionary<string, int> Keys { get; set; } = new();
    
    public bool IsUnique { get; set; }
    
    public bool IsSparse { get; set; }
    
    public long SizeInBytes { get; set; }
}
