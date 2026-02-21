namespace DashboardAnalytics.Models;

public class ServiceDatabase
{
    public string ServiceName { get; set; } = string.Empty;
    
    public string DatabaseName { get; set; } = string.Empty;
    
    public string ConnectionString { get; set; } = string.Empty;
    
    public int Port { get; set; }
    
    public List<TableInfo> Tables { get; set; } = new();
    
    public DatabaseStats Stats { get; set; } = new();
    
    public bool IsConnected { get; set; }
    
    public string? ErrorMessage { get; set; }
}
