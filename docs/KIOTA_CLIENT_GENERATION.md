# API Client Generation with Kiota

This project uses [Microsoft Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview) to automatically generate strongly-typed TypeScript clients from OpenAPI (Swagger) specifications.

## 🎯 Why Kiota?

- **Type Safety**: Automatically generates TypeScript types that match your C# DTOs exactly
- **Auto camelCase Conversion**: Handles PascalCase (C#) → camelCase (TypeScript) automatically
- **Always in Sync**: Regenerate clients when your backend APIs change
- **IntelliSense**: Full IDE support with autocomplete for all API endpoints and models
- **No Manual Maintenance**: Eliminate manual type definitions and API service code

## 📦 What's Generated

Kiota generates TypeScript clients for all microservices:

- **Dashboard Service** (Port 5005) - Analytics, metrics, KPIs, alerts
- **User Management Service** (Port 5001) - Authentication, users, roles
- **Inventory Service** (Port 5002) - Products, categories, stock movements
- **Sales Service** (Port 5003) - Orders, customers, invoices
- **Financial Service** (Port 5004) - Transactions, budgets

Generated files are located in: `frontend/src/generated/clients/<service-name>/`

## 🚀 Quick Start

### Prerequisites

1. All backend services must be running (they expose OpenAPI specs at `/swagger/v1/swagger.json`)
2. Run the watch-all-services task or start services individually

### Generate All Clients

```bash
# From the root directory
npm run generate:api --prefix frontend

# Or using PowerShell directly
pwsh ./scripts/generate-api-clients.ps1
```

### Generate Client for Single Service

```bash
npm run generate:api:service dashboard --prefix frontend

# Or using PowerShell
pwsh ./scripts/generate-api-clients.ps1 -Service dashboard
```

### Check Service Availability

```bash
npm run check:services --prefix frontend

# Or using PowerShell
pwsh ./scripts/generate-api-clients.ps1 -CheckServices
```

## 🔧 Configuration

### Service Ports (kiota-config.json)

```json
{
  "dashboard": "http://localhost:5005/swagger/v1/swagger.json",
  "user-management": "http://localhost:5001/swagger/v1/swagger.json",
  "inventory": "http://localhost:5002/swagger/v1/swagger.json",
  "sales": "http://localhost:5003/swagger/v1/swagger.json",
  "financial": "http://localhost:5004/swagger/v1/swagger.json"
}
```

### Output Locations

- Dashboard → `frontend/src/generated/clients/dashboard/`
- User Management → `frontend/src/generated/clients/user-management/`
- Inventory → `frontend/src/generated/clients/inventory/`
- Sales → `frontend/src/generated/clients/sales/`
- Financial → `frontend/src/generated/clients/financial/`

## 💻 Using Generated Clients

### Example: Dashboard Metrics

**Backend DTO (C# - PascalCase):**
```csharp
public record DashboardMetricsResponse(
    decimal TotalRevenue,
    int TotalOrders,
    int TotalCustomers,
    DateTime LastUpdated
);
```

**Generated TypeScript Interface (camelCase):**
```typescript
export interface DashboardMetricsResponse {
    totalRevenue?: number;
    totalOrders?: number;
    totalCustomers?: number;
    lastUpdated?: Date;
}
```

**Using in Your Service:**
```typescript
import type { DashboardMetricsResponse } from '../generated/clients/dashboard/models';
import apiService from './api.service';

async getDashboardMetrics(): Promise<DashboardMetricsResponse> {
  const response = await apiService.get<ApiResponse<DashboardMetricsResponse>>(
    '/api/v1/dashboard/metrics'
  );
  return response.data;
}
```

**In Your Component:**
```typescript
import type { DashboardMetricsResponse } from '../../generated/clients/dashboard/models';

interface AnalyticsState {
  dashboardSummary: DashboardMetricsResponse | null;
}

// Full IntelliSense support!
<p>{dashboardSummary.totalRevenue}</p>
<p>{dashboardSummary.totalOrders}</p>
<p>{dashboardSummary.totalCustomers}</p>
```

### Example: Sales Order

```typescript
import type { OrderResponse, CreateOrderRequest } from '../generated/clients/sales/models';

// Create order with type checking
const createOrder = async (request: CreateOrderRequest): Promise<OrderResponse> => {
  const response = await apiService.post<ApiResponse<OrderResponse>>(
    '/api/v1/orders',
    request
  );
  return response.data;
};
```

## 🔄 Workflow

### During Development

1. **Make backend changes** to your C# DTOs or Controllers
2. **Save and build** (dotnet watch will rebuild automatically)
3. **Regenerate clients**: `npm run generate:api --prefix frontend`
4. **TypeScript will show errors** if frontend code needs updates
5. **Fix the TypeScript errors** with full IntelliSense support

### Before Committing

```bash
# Ensure all services are running
npm run check:services --prefix frontend

# Regenerate all clients
npm run generate:api --prefix frontend

# Verify no TypeScript errors
cd frontend
npm run build
```

## 📝 Generated Client Structure

```
frontend/src/generated/clients/dashboard/
├── dashboardClient.ts       # Main client entry point
├── api/                      # API path builders
│   └── v1/
│       ├── dashboard/
│       │   ├── metrics/
│       │   └── sales/
│       ├── kpis/
│       └── alerts/
├── models/
│   └── index.ts             # All TypeScript interfaces
├── .kiota.log               # Generation log (gitignored)
└── kiota-lock.json          # Lock file (gitignored)
```

## 🛠️ Troubleshooting

### Service Not Running

```
⚠️ Service not running, skipping...
```

**Solution**: Start the service with VS Code tasks or manually:
```bash
dotnet watch run --project services/dashboard/DashboardAnalytics
```

### Port Conflicts

If services are on different ports, update `scripts/generate-api-clients.ps1` and `kiota-config.json`

### Kiota Not Found

```bash
# Install Kiota as a local tool
dotnet tool restore

# Verify installation
dotnet kiota --version
```

### TypeScript Errors After Generation

This is expected! The generated types will reveal mismatches between backend and frontend. Fix them one by one.

## 📚 Advanced Usage

### Custom API Client with Authentication

```typescript
import { createDashboardClient } from '../generated/clients/dashboard/dashboardClient';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';

const adapter = new FetchRequestAdapter();
adapter.baseUrl = 'http://localhost:5000';

// Add auth token
const originalFetch = adapter.fetch;
adapter.fetch = async (url, init) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    init.headers = {
      ...init.headers,
      Authorization: `Bearer ${token}`,
    };
  }
  return originalFetch(url, init);
};

const client = createDashboardClient(adapter);
const metrics = await client.api.v1.dashboard.metrics.get();
```

### Filtering Generated Code

Edit `kiota-config.json` to include/exclude specific API paths:

```json
{
  "includePatterns": ["**/v1/**"],
  "excludePatterns": ["**/internal/**"]
}
```

## 🔗 Resources

- [Kiota Documentation](https://learn.microsoft.com/en-us/openapi/kiota/)
- [Kiota TypeScript Guide](https://learn.microsoft.com/en-us/openapi/kiota/quickstarts/typescript)
- [OpenAPI Specification](https://swagger.io/specification/)

## ⚙️ Scripts Reference

| Command | Description |
|---------|-------------|
| `npm run generate:api` | Generate all API clients |
| `npm run generate:api:service <name>` | Generate single service client |
| `npm run check:services` | Check which services are running |
| `pwsh ./scripts/generate-api-clients.ps1` | Direct PowerShell execution |
| `pwsh ./scripts/generate-api-clients.ps1 -Service dashboard` | Generate one service |
| `pwsh ./scripts/generate-api-clients.ps1 -CheckServices` | Service health check |

## 💡 Tips

1. **Commit Generated Code**: Keep generated clients in version control for better collaboration
2. **Regenerate Regularly**: Make it part of your workflow after backend changes
3. **Use Types Everywhere**: Import and use generated types instead of `any`
4. **Check Build Errors**: Generated types will reveal API mismatches immediately
5. **CI/CD Integration**: Add client generation to your build pipeline

## 🐛 Common Issues

### Issue: `activeCustomers` property doesn't exist

**Before (Wrong):**
```typescript
interface DashboardSummary {
  activeCustomers: number;  // Backend doesn't have this!
}
```

**After (Correct - Using Generated Types):**
```typescript
import type { DashboardMetricsResponse } from '../generated/clients/dashboard/models';

// TypeScript will show you the actual properties:
// - totalCustomers
// - totalOrders
// - totalRevenue
// etc.
```

### Issue: Wrong endpoint

**Before:**
```typescript
await apiService.get('/api/v1/dashboard');  // 404 Not Found
```

**After:**
```typescript
await apiService.get('/api/v1/dashboard/metrics');  // ✅ Correct
```

The generated client structure mirrors your actual API paths!
