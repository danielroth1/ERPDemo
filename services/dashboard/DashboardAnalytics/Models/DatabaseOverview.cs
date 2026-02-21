namespace DashboardAnalytics.Models;

public class DatabaseOverview
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public List<ServiceDatabase> Services { get; set; } = new();
    
    public DatabaseStats TotalStats { get; set; } = new();
}
