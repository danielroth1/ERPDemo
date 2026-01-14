using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DashboardAnalytics.Models;

public class DatabaseOverview
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public List<ServiceDatabase> Services { get; set; } = new();
    
    public DatabaseStats TotalStats { get; set; } = new();
}

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

public class IndexInfo
{
    public string Name { get; set; } = string.Empty;
    
    public Dictionary<string, int> Keys { get; set; } = new();
    
    public bool IsUnique { get; set; }
    
    public bool IsSparse { get; set; }
    
    public long SizeInBytes { get; set; }
}

public class DatabaseStats
{
    public long TotalCollections { get; set; }
    
    public long TotalDocuments { get; set; }
    
    public long TotalSizeInBytes { get; set; }
    
    public long TotalIndexes { get; set; }
    
    public double AverageDocumentSize { get; set; }
}

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
