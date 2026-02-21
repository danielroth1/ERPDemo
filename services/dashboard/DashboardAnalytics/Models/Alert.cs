using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DashboardAnalytics.Models;

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
