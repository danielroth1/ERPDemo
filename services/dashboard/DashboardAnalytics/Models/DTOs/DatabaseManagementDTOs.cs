namespace DashboardAnalytics.Models.DTOs;

// Request DTOs
public record DatabaseOverviewRequest(
    bool IncludeSampleDocuments = true,
    bool IncludeSchemaInference = false,
    int MaxSampleSize = 1000
);

public record ExecuteQueryRequest(
    string DatabaseName,
    string CollectionName,
    string Query,
    string QueryType, // Find, Aggregate, Count
    int? Limit = 100,
    int? Skip = 0
);

public record SearchDatabaseRequest(
    string? SearchTerm,
    string? ServiceName,
    string? CollectionName,
    long? MinDocumentCount,
    long? MaxDocumentCount,
    long? MinSizeInBytes,
    long? MaxSizeInBytes
);

// Response DTOs
public record DatabaseOverviewResponse(
    string Id,
    DateTime GeneratedAt,
    List<ServiceDatabaseResponse> Services,
    DatabaseStatsResponse TotalStats,
    int CacheTimeSeconds
);

public record ServiceDatabaseResponse(
    string ServiceName,
    string DatabaseName,
    string ConnectionString,
    int Port,
    List<CollectionInfoResponse> Collections,
    DatabaseStatsResponse Stats,
    bool IsConnected,
    string? ErrorMessage
);

public record CollectionInfoResponse(
    string Name,
    long DocumentCount,
    long SizeInBytes,
    double AverageSizeInBytes,
    List<IndexInfoResponse> Indexes,
    string? SampleDocument,
    Dictionary<string, string>? Schema
);

public record IndexInfoResponse(
    string Name,
    Dictionary<string, int> Keys,
    bool IsUnique,
    bool IsSparse,
    long SizeInBytes
);

public record DatabaseStatsResponse(
    long TotalCollections,
    long TotalDocuments,
    long TotalSizeInBytes,
    long TotalIndexes,
    double AverageDocumentSize
);

public record QueryExecutionResponse(
    string Id,
    bool IsSuccessful,
    string? ErrorMessage,
    List<string> Results,
    int ResultCount,
    long ExecutionTimeMs,
    DateTime ExecutedAt
);

public record QueryExecutionHistoryResponse(
    string Id,
    string UserEmail,
    string DatabaseName,
    string CollectionName,
    string Query,
    string QueryType,
    bool IsSuccessful,
    int ResultCount,
    long ExecutionTimeMs,
    DateTime ExecutedAt
);

public record DatabaseAlertResponse(
    string Id,
    string ServiceName,
    string DatabaseName,
    string CollectionName,
    string AlertType,
    string Message,
    string Severity,
    Dictionary<string, object> Metadata,
    bool IsResolved,
    DateTime CreatedAt,
    DateTime? ResolvedAt
);

public record DatabaseSearchResult(
    string ServiceName,
    string DatabaseName,
    string CollectionName,
    long DocumentCount,
    long SizeInBytes,
    List<string> MatchedFields
);

// GraphQL Subscription DTOs
public record DatabaseUpdateEvent(
    string ServiceName,
    string DatabaseName,
    string EventType, // CollectionCreated, DocumentInserted, IndexCreated
    string CollectionName,
    DateTime Timestamp,
    Dictionary<string, object>? Metadata
);
