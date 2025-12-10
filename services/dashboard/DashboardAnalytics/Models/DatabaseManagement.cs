using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DashboardAnalytics.Models;

public class DatabaseOverview
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
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
    public List<CollectionInfo> Collections { get; set; } = new();
    public DatabaseStats Stats { get; set; } = new();
    public bool IsConnected { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CollectionInfo
{
    public string Name { get; set; } = string.Empty;
    public long DocumentCount { get; set; }
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

public class QueryExecution
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public string QueryType { get; set; } = string.Empty; // Find, Aggregate, Count
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public int ResultCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

public class DatabaseAlert
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    
    public string ServiceName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string CollectionName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty; // LowStorage, HighDocumentCount, SlowQuery
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Info, Warning, Critical
    public Dictionary<string, object> Metadata { get; set; } = new();
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
