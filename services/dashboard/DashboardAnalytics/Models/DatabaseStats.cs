namespace DashboardAnalytics.Models;

public class DatabaseStats
{
    public long TotalCollections { get; set; }
    
    public long TotalDocuments { get; set; }
    
    public long TotalSizeInBytes { get; set; }
    
    public long TotalIndexes { get; set; }
    
    public double AverageDocumentSize { get; set; }
}
