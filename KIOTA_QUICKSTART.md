# API Client Generation - Quick Reference

## Generate All API Clients

```bash
npm run generate:api --prefix frontend
```

## Generate Single Service Client

```bash
npm run generate:api:service dashboard --prefix frontend
npm run generate:api:service sales --prefix frontend
```

## Check Services Status

```bash
npm run check:services --prefix frontend
```

## Prerequisites

- All backend services must be running
- Start them with VS Code task: `watch-all-services`

## What Gets Generated?

TypeScript clients with full type safety for:
- Dashboard Service (analytics, KPIs, metrics)
- Sales Service (orders, customers, invoices)
- Inventory Service (products, stock)
- Financial Service (transactions, budgets)
- User Management Service (auth, users, roles)

## Usage Example

```typescript
// Import generated types
import type { DashboardMetricsResponse } from './generated/clients/dashboard/models';

// Use in your service
async getDashboardMetrics(): Promise<DashboardMetricsResponse> {
  const response = await apiService.get('/api/v1/dashboard/metrics');
  return response.data;
}

// Full IntelliSense support in components!
<p>Revenue: {dashboardSummary.totalRevenue}</p>
<p>Orders: {dashboardSummary.totalOrders}</p>
```

## More Information

See [docs/KIOTA_CLIENT_GENERATION.md](./docs/KIOTA_CLIENT_GENERATION.md) for complete documentation.

## Workflow

1. Make backend changes to DTOs/Controllers
2. Backend rebuilds (dotnet watch)
3. Run: `npm run generate:api --prefix frontend`
4. TypeScript shows errors if updates needed
5. Fix errors with full IntelliSense

## Benefits

✅ **No manual type definitions**
✅ **PascalCase → camelCase automatically**
✅ **Always in sync with backend**
✅ **Catches mismatches at compile time**
✅ **Full IDE autocomplete**
