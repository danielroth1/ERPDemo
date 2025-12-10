# Dashboard & Analytics Service

The Dashboard & Analytics service provides real-time metrics aggregation, KPI tracking, alert management, and comprehensive analytics for the ERP system. It consumes events from all other microservices via Kafka and exposes the data through REST API, GraphQL, and SignalR for real-time updates.

## Features

### Real-Time Event Processing
- **Kafka Consumers**: Consumes events from User Management, Inventory, Sales & Orders, and Financial services
- **Event Aggregation**: Processes and aggregates metrics in real-time
- **Automatic Updates**: Dashboard metrics update automatically as events arrive
- **Multi-Topic Support**: Listens to 10 different Kafka topics

### Metrics & KPIs
- **Dashboard Metrics**: Total revenue, expenses, orders, products, users, inventory value
- **KPI Management**: Create, track, and monitor Key Performance Indicators
- **Status Tracking**: Automatic KPI status calculation (OnTrack/NeedsAttention/Critical)
- **Historical Tracking**: Previous values and percentage change calculations

### Alert System
- **Automated Alerts**: Low stock and budget exceeded alerts
- **Severity Levels**: Info, Warning, Error, Critical
- **Read/Unread Tracking**: Mark alerts as read
- **Real-Time Notifications**: SignalR push notifications

### Real-Time Updates
- **SignalR Hub**: WebSocket connection for live dashboard updates
- **GraphQL Subscriptions**: Subscribe to metric changes via GraphQL
- **Event Broadcasting**: All connected clients receive updates instantly

### Caching
- **Memory Cache**: Frequently accessed metrics cached for 5-10 minutes
- **Cache Invalidation**: Automatic invalidation on relevant events
- **Performance Optimization**: Reduced database queries

### Analytics
- **Sales Overview**: Orders, revenue, average order value, top products
- **Inventory Overview**: Stock levels, low stock items, inventory value
- **Financial Summary**: Revenue, expenses, profit margin, trends
- **Chart Data**: Time-series data for visualizations

## Architecture

### Technologies
- **ASP.NET Core 8**: Web API framework
- **MongoDB 7**: Document database for metrics and KPIs
- **Apache Kafka**: Event streaming from all services
- **SignalR**: Real-time WebSocket connections
- **HotChocolate GraphQL**: GraphQL API with subscriptions
- **Memory Cache**: In-memory caching for performance
- **Prometheus**: Metrics collection
- **Serilog**: Structured logging
- **Swagger/OpenAPI**: API documentation

### Domain Models

#### DashboardMetrics
```csharp
public class DashboardMetrics
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public MetricType Type { get; set; }  // UserCount, ProductCount, Revenue, etc.
    public string Label { get; set; }
    public decimal Value { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

#### KPI
```csharp
public class KPI
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal PreviousValue { get; set; }
    public decimal PercentageChange { get; set; }
    public KPIStatus Status { get; set; }  // OnTrack, NeedsAttention, Critical
    public DateTime LastUpdated { get; set; }
}
```

#### Alert
```csharp
public class Alert
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public AlertSeverity Severity { get; set; }  // Info, Warning, Error, Critical
    public string Source { get; set; }  // Inventory, Financial, etc.
    public Dictionary<string, object> Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Kafka Event Processing

The service consumes events from multiple topics:

| Topic | Event Type | Processing Action |
|-------|------------|-------------------|
| `users.user.created` | User registered | Increment user count |
| `users.user.updated` | User updated | Update user metrics |
| `inventory.product.created` | Product added | Increment product count |
| `inventory.product.updated` | Product updated | Update product metrics |
| `inventory.stock.low` | Low stock alert | Create alert, update low stock count |
| `sales.order.created` | Order placed | Increment order count, add to revenue |
| `sales.order.updated` | Order status changed | Update order metrics |
| `sales.invoice.paid` | Invoice paid | Add to revenue |
| `financial.transaction.created` | Transaction posted | Update revenue/expenses based on type |
| `financial.budget.exceeded` | Budget exceeded | Create critical alert |

### Event Flow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   User      │────▶│    Kafka    │────▶│  Dashboard  │
│ Management  │     │             │     │   Service   │
└─────────────┘     │             │     └──────┬──────┘
                    │             │            │
┌─────────────┐     │   Topics    │            │
│  Inventory  │────▶│             │◀───────────┤
└─────────────┘     │             │            │
                    │             │            ▼
┌─────────────┐     │             │     ┌─────────────┐
│Sales/Orders │────▶│             │     │   MongoDB   │
└─────────────┘     │             │     │  Metrics DB │
                    │             │     └─────────────┘
┌─────────────┐     │             │            │
│  Financial  │────▶│             │            │
└─────────────┘     └─────────────┘            ▼
                                        ┌─────────────┐
                                        │   SignalR   │
                                        │   Clients   │
                                        └─────────────┘
```

## API Endpoints

### REST API

#### Dashboard Endpoints (`/api/v1/dashboard`)

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/v1/dashboard/metrics` | Get overall dashboard metrics | All authenticated |
| GET | `/api/v1/dashboard/sales` | Get sales overview | All authenticated |
| GET | `/api/v1/dashboard/inventory` | Get inventory overview | All authenticated |
| GET | `/api/v1/dashboard/financial` | Get financial summary | Manager, Admin |

#### KPIs Endpoints (`/api/v1/kpis`)

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/api/v1/kpis` | Create new KPI | Manager, Admin |
| GET | `/api/v1/kpis/{id}` | Get KPI by ID | All authenticated |
| GET | `/api/v1/kpis` | List all KPIs | All authenticated |
| PUT | `/api/v1/kpis/{id}` | Update KPI | Manager, Admin |
| DELETE | `/api/v1/kpis/{id}` | Delete KPI | Admin |

#### Alerts Endpoints (`/api/v1/alerts`)

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/v1/alerts/{id}` | Get alert by ID | All authenticated |
| GET | `/api/v1/alerts` | List all alerts (paginated) | All authenticated |
| GET | `/api/v1/alerts/unread` | Get unread alerts | All authenticated |
| PUT | `/api/v1/alerts/{id}/read` | Mark alert as read | All authenticated |
| DELETE | `/api/v1/alerts/{id}` | Delete alert | Manager, Admin |

### GraphQL API (`/graphql`)

#### Queries
```graphql
type Query {
  getDashboardMetrics: DashboardMetricsResponse!
  getSalesOverview: SalesOverviewResponse!
  getInventoryOverview: InventoryOverviewResponse!
  getFinancialSummary: FinancialSummaryResponse!
  getAllKPIs: [KPIResponse!]!
  getKPIById(id: ID!): KPIResponse
  getAllAlerts(page: Int!, pageSize: Int!): [AlertResponse!]!
  getUnreadAlerts: [AlertResponse!]!
}
```

#### Mutations
```graphql
type Mutation {
  createKPI(request: CreateKPIRequest!): KPIResponse!
  updateKPI(id: ID!, request: UpdateKPIRequest!): KPIResponse
  deleteKPI(id: ID!): Boolean!
  markAlertAsRead(id: ID!): Boolean!
  deleteAlert(id: ID!): Boolean!
}
```

#### Subscriptions
```graphql
type Subscription {
  onDashboardUpdate: DashboardMetricsResponse!
  onAlertReceived: AlertResponse!
  onKPIUpdated: KPIResponse!
}
```

### SignalR Hub (`/dashboardHub`)

#### Hub Methods
- `SubscribeToMetrics()`: Subscribe to real-time metric updates
- `UnsubscribeFromMetrics()`: Unsubscribe from updates

#### Client Events
- `ReceiveDashboardUpdate`: Receives dashboard metric updates
- `ReceiveAlert`: Receives new alerts in real-time

## Configuration

### appsettings.json
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://mongodb:27017",
    "DatabaseName": "erp_dashboard"
  },
  "Jwt": {
    "Secret": "your-secret-key",
    "Issuer": "erp-dashboard-service",
    "Audience": "erp-clients"
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "ConsumerGroupId": "dashboard-service-group"
  }
}
```

## MongoDB Collections

### metrics
- **Purpose**: Store time-series metrics data
- **Indexes**: 
  - `Type, Timestamp` (compound, for time-series queries)
  - `Timestamp` (for cleanup/archiving)

### kpis
- **Purpose**: Store KPI definitions and current values
- **Indexes**: 
  - `Name` (for lookups)
  - `LastUpdated` (for recent KPIs)

### alerts
- **Purpose**: Store system alerts
- **Indexes**: 
  - `CreatedAt` (for sorting)
  - `IsRead` (for unread queries)
  - `Severity` (for filtering)

### charts
- **Purpose**: Store pre-computed chart data
- **Indexes**: 
  - `ChartId` (for lookups)
  - `GeneratedAt` (for cache invalidation)

## Development

### Prerequisites
- .NET 8 SDK
- MongoDB 7+
- Apache Kafka 7.5+
- Docker & Docker Compose

### Running Locally
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

### Running with Docker
```bash
# Build image
docker build -t erp-dashboard:latest .

# Run container
docker run -p 8080:8080 \
  -e MongoDb__ConnectionString=mongodb://mongodb:27017 \
  -e Kafka__BootstrapServers=kafka:9092 \
  erp-dashboard:latest
```

## Usage Examples

### REST API: Get Dashboard Metrics
```bash
curl -X GET http://localhost:8080/api/v1/dashboard/metrics \
  -H "Authorization: Bearer {token}"
```

### GraphQL: Query Dashboard Metrics
```graphql
query {
  getDashboardMetrics {
    totalRevenue
    totalExpenses
    netIncome
    totalOrders
    totalProducts
    lowStockProducts
    activeUsers
    lastUpdated
  }
}
```

### GraphQL: Subscribe to Updates
```graphql
subscription {
  onDashboardUpdate {
    totalRevenue
    totalOrders
    lastUpdated
  }
}
```

### SignalR: Connect and Subscribe (JavaScript)
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:8080/dashboardHub", {
    accessTokenFactory: () => yourJwtToken
  })
  .build();

connection.on("ReceiveDashboardUpdate", (update) => {
  console.log("Dashboard updated:", update);
});

connection.on("ReceiveAlert", (alert) => {
  console.log("New alert:", alert);
});

await connection.start();
await connection.invoke("SubscribeToMetrics");
```

### Create KPI via REST
```bash
curl -X POST http://localhost:8080/api/v1/kpis \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Monthly Revenue Goal",
    "description": "Target $100k monthly revenue",
    "targetValue": 100000
  }'
```

### Update KPI
```bash
curl -X PUT http://localhost:8080/api/v1/kpis/{kpiId} \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "currentValue": 85000
  }'
```

## Caching Strategy

- **Dashboard Metrics**: 5 minutes TTL, invalidated on order/transaction events
- **Sales Overview**: 10 minutes TTL, invalidated on order events
- **Inventory Overview**: 10 minutes TTL, invalidated on low stock events
- **Financial Summary**: 10 minutes TTL, invalidated on transaction events

## Performance Considerations

1. **Event Processing**: Async processing with background service
2. **Batching**: Metrics can be batched for bulk inserts
3. **Indexing**: Proper MongoDB indexes for fast queries
4. **Caching**: Reduces database load for frequently accessed data
5. **In-Memory Counters**: Fast access to current counts

## Error Handling
- `400 Bad Request`: Invalid KPI data or request format
- `401 Unauthorized`: Missing or invalid JWT token
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: KPI or alert not found
- `500 Internal Server Error`: Database, Kafka, or SignalR connectivity issues

## Monitoring

### Prometheus Metrics
Available at `/metrics`:
- `http_requests_total` - Total HTTP requests
- `http_request_duration_seconds` - Request duration
- Custom business metrics for events processed

### Health Checks
- **Liveness**: `GET /health/live`
- **Readiness**: `GET /health/ready` (checks MongoDB connectivity)

## Security
- **JWT Authentication**: Required for all endpoints
- **Role-Based Authorization**: Manager/Admin roles for sensitive operations
- **SignalR Authentication**: JWT token passed via query string
- **CORS**: Configured for frontend access

## Real-Time Architecture

The service uses multiple real-time communication patterns:

1. **Kafka → Service**: Event-driven updates from other services
2. **Service → SignalR**: WebSocket push to connected clients
3. **Service → GraphQL Subscriptions**: GraphQL over WebSocket
4. **In-Memory → SignalR**: Immediate broadcast of metric changes

## Business Logic

### KPI Status Calculation
- **OnTrack**: Progress ≥ 90% of target
- **NeedsAttention**: Progress between 70-89% of target
- **Critical**: Progress < 70% of target

### Alert Severity Rules
- **Info**: General notifications
- **Warning**: Low stock alerts (stock < reorder level)
- **Error**: Budget exceeded by < 20%
- **Critical**: Budget exceeded by ≥ 20%

## License
Part of the ERP Demo Application - MIT License
