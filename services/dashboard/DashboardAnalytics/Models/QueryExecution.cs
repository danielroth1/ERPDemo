using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DashboardAnalytics.Models;

[Table("query_executions")]
[Index(nameof(UserId))]
[Index(nameof(ExecutedAt))]
public class QueryExecution
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public string UserId { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string UserEmail { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string DatabaseName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string CollectionName { get; set; } = string.Empty;
    
    public string Query { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string QueryType { get; set; } = string.Empty; // Select, Insert, Update, Delete
    
    public bool IsSuccessful { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public int ResultCount { get; set; }
    
    public long ExecutionTimeMs { get; set; }
    
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
