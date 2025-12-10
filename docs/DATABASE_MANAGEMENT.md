# Database Management Service

A comprehensive MongoDB database management and monitoring system integrated into the ERP Dashboard service.

## Features

### 🔍 Real-Time Database Overview
- **Multi-Service Monitoring**: Monitors all 5 MongoDB databases across services
  - User Management (`erp_users`)
  - Inventory (`erp_inventory`)
  - Sales (`erp_sales`)
  - Financial (`erp_financial`)
  - Dashboard (`erp_dashboard`)
- **Live Statistics**: Total collections, documents, size, indexes, and averages
- **Connection Status**: Real-time health checks for each database
- **Auto-refresh**: Optional automatic updates with GraphQL subscriptions

### 💾 Caching System
- **Redis Integration**: Distributed caching for improved performance
- **5-minute Cache TTL**: Configurable cache expiration
- **Force Refresh**: Manual cache invalidation on demand
- **Smart Caching**: Individual service and global overview caching

### 📊 Collection Details
- **Document Counts**: Real-time document statistics
- **Size Metrics**: Collection size and average document size
- **Index Information**: View all indexes with unique/sparse flags
- **Schema Inference**: Automatic schema detection from sample documents
- **Sample Documents**: Preview actual document structure

### 🔎 Advanced Search & Filtering
- **Full-Text Search**: Search across collection names and fields
- **Service Filtering**: Filter by specific service
- **Collection Filtering**: Filter by collection name
- **Document Count Range**: Min/max document count filters
- **Size Range Filters**: Filter by collection size in bytes
- **Multi-Field Matching**: Highlights matched fields in results

### 💻 Query Execution (Admin Only)
- **MongoDB Query Support**:
  - **Find**: Standard document queries
  - **Count**: Document counting with filters
  - **Aggregate**: Pipeline aggregations
- **Safety Features**:
  - Query validation (blocks dangerous keywords)
  - JSON validation
  - Result limits (default: 100, max: 1000)
  - Execution timeout protection
- **Query History**: Audit trail of all executed queries
- **Performance Metrics**: Execution time tracking

### 🚨 Intelligent Alerts
- **High Document Count**: Alerts when collections exceed 1M documents
- **Large Collection Size**: Alerts for collections > 1GB
- **Severity Levels**: Info, Warning, Critical
- **Auto-Detection**: Automatic alert generation during overview refresh
- **Resolution Tracking**: Mark alerts as resolved

### 📡 GraphQL Subscriptions
- **Real-Time Updates**: WebSocket-based live data
- **Event Types**:
  - `DatabaseUpdates`: Collection changes
  - `DatabaseRefreshed`: Cache refresh events
  - `QueryExecuted`: Query execution events
- **Auto-Reconnect**: Automatic reconnection on connection loss

## Architecture

### Backend (C# / ASP.NET Core 8)

#### Models
- `DatabaseOverview`: Complete database snapshot
- `ServiceDatabase`: Individual service database info
- `CollectionInfo`: Collection metadata
- `IndexInfo`: Index specifications
- `QueryExecution`: Query execution records
- `DatabaseAlert`: Alert tracking

#### Services
- `DatabaseOverviewService`: Core database introspection
- `PublishDatabaseUpdateService`: GraphQL subscription publisher

#### Controllers
- `DatabaseController`: REST API endpoints

#### GraphQL
- `DatabaseQuery`: GraphQL queries
- `DatabaseMutation`: GraphQL mutations
- `DatabaseSubscription`: Real-time subscriptions

### Frontend (React / TypeScript)

#### Components
- `DatabaseOverviewPage`: Main page with tabs
- `DatabaseStats`: Aggregate statistics cards
- `ServiceDatabaseCard`: Expandable service cards
- `DatabaseSearch`: Advanced search interface
- `QueryExecutor`: Query execution panel
- `DatabaseAlerts`: Alert management

#### Services
- `databaseService`: API client for database operations
- `useDatabaseSubscription`: GraphQL subscription hook

## API Endpoints

### REST API

```
GET    /api/v1/database/overview              # Get complete overview
GET    /api/v1/database/service/{name}        # Get service database
POST   /api/v1/database/search                # Search databases
POST   /api/v1/database/query                 # Execute query (Admin)
GET    /api/v1/database/query-history         # Query history
GET    /api/v1/database/alerts                # Get alerts
POST   /api/v1/database/cache/clear           # Clear cache (Admin)
```

### GraphQL API

```graphql
# Queries
query GetDatabaseOverview($forceRefresh: Boolean = false) {
  getDatabaseOverview(forceRefresh: $forceRefresh) {
    id
    generatedAt
    services {
      serviceName
      databaseName
      isConnected
      collections {
        name
        documentCount
        sizeInBytes
      }
    }
    totalStats {
      totalCollections
      totalDocuments
      totalSizeInBytes
    }
  }
}

# Mutations
mutation ExecuteQuery($request: ExecuteQueryRequest!) {
  executeQuery(request: $request) {
    id
    isSuccessful
    results
    executionTimeMs
  }
}

# Subscriptions
subscription OnDatabaseUpdate {
  onDatabaseUpdate {
    serviceName
    databaseName
    eventType
    collectionName
    timestamp
  }
}
```

## Configuration

### Backend (appsettings.json)

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://mongodb:27017",
    "DatabaseName": "erp_dashboard"
  },
  "Redis": {
    "ConnectionString": "redis:6379"
  }
}
```

### Frontend (Environment)

```typescript
// GraphQL WebSocket URL
const wsUrl = 'ws://localhost:5005/graphql';

// REST API base URL
const apiUrl = 'http://localhost:5005';
```

## Security

### Role-Based Access Control

- **Admin Only**:
  - Query execution
  - Cache clearing
  - Full database overview
  
- **Manager + Admin**:
  - Service database details
  - Search functionality
  - Query history (own queries)
  - Alert viewing

### Query Safety

- **Blocked Keywords**: `$where`, `eval`, `function`, `javascript`
- **JSON Validation**: All queries must be valid JSON
- **Audit Logging**: All queries logged with user info
- **Result Limits**: Enforced maximum result count

## Usage Examples

### Get Database Overview

```typescript
import { databaseService } from './services/database.service';

// Get cached overview
const overview = await databaseService.getDatabaseOverview();

// Force refresh
const freshOverview = await databaseService.getDatabaseOverview(true);
```

### Search Databases

```typescript
const results = await databaseService.searchDatabases({
  searchTerm: 'user',
  serviceName: 'User Management',
  minDocumentCount: 100,
});
```

### Execute Query

```typescript
const result = await databaseService.executeQuery({
  databaseName: 'erp_inventory',
  collectionName: 'products',
  query: '{ "isActive": true }',
  queryType: 'Find',
  limit: 50,
});
```

### Subscribe to Updates

```typescript
const { subscribe, isConnected } = useDatabaseSubscription({
  onData: (event) => {
    console.log('Database update:', event);
  },
});

useEffect(() => {
  if (isConnected) {
    const unsubscribe = subscribe('DatabaseUpdates');
    return () => unsubscribe?.();
  }
}, [isConnected]);
```

## Performance Considerations

### Caching Strategy
- **5-minute TTL**: Balances freshness with performance
- **Lazy Loading**: Only fetches data when needed
- **Selective Refresh**: Can refresh individual services

### Query Optimization
- **Connection Pooling**: MongoDB connection reuse
- **Result Limiting**: Default 100 documents
- **Timeout Protection**: Prevents long-running queries

### Memory Usage
- **Sample Documents**: Limited to 1000 characters
- **Result Pagination**: Skip/limit support
- **Cache Compression**: Redis compression enabled

## Monitoring & Alerts

### Automatic Alerts

```csharp
// High document count (> 1M)
CreateAlert("HighDocumentCount", "Warning");

// Large collection (> 1GB)
CreateAlert("LargeCollectionSize", "Warning");
```

### Alert Resolution

```csharp
// Mark alert as resolved
alert.IsResolved = true;
alert.ResolvedAt = DateTime.UtcNow;
```

## Development

### Adding New Service Databases

1. Update `_serviceDatabases` dictionary in `DatabaseOverviewService.cs`:

```csharp
private readonly Dictionary<string, string> _serviceDatabases = new()
{
    { "New Service", "erp_newservice" }
};
```

2. Restart the service - no frontend changes needed!

### Custom Alerts

Add custom alert logic in `CheckAndCreateAlertsAsync`:

```csharp
// Custom alert example
if (collection.DocumentCount == 0 && collection.Name != "system.indexes")
{
    await CreateAlertIfNotExistsAsync(
        serviceName,
        databaseName,
        collection.Name,
        "EmptyCollection",
        $"Collection {collection.Name} is empty",
        "Info"
    );
}
```

## Troubleshooting

### Connection Issues

```bash
# Test MongoDB connection
docker exec erp-mongodb mongosh -u admin -p admin123

# Test Redis connection
docker exec erp-redis redis-cli ping
```

### Cache Not Working

```bash
# Check Redis logs
docker logs erp-redis

# Clear cache manually
curl -X POST http://localhost:5005/api/v1/database/cache/clear \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### GraphQL Subscription Not Connecting

1. Verify WebSocket support in browser
2. Check JWT token in localStorage
3. Inspect browser console for errors
4. Verify backend WebSocket endpoint

## Future Enhancements

- [ ] Collection backup/restore triggers
- [ ] Query performance profiling
- [ ] Custom dashboard widgets
- [ ] Export to CSV/JSON
- [ ] Scheduled reports
- [ ] Role-based collection access
- [ ] Query templates library
- [ ] Index optimization suggestions
- [ ] Historical trend analysis
- [ ] Automated cleanup jobs

## License

Part of the ERP System - Internal Use Only
