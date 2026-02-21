using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DashboardAnalytics.Models;

[Table("database_alerts")]
public class DatabaseAlert
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string DatabaseName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string CollectionName { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string AlertType { get; set; } = string.Empty; // LowStorage, HighRowCount, SlowQuery
    
    public string Message { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Severity { get; set; } = string.Empty; // Info, Warning, Critical
    
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public bool IsResolved { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ResolvedAt { get; set; }
}
