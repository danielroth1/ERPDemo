# Multiple Apollo Clients Guide

## Overview

This app uses **multiple Apollo Client instances** to connect to different GraphQL endpoints (`/dashboard/graphql`, `/sales/graphql`, etc.). Here's how to work with them.

## ✅ Current Implementation: Hook-Based Client Selection

### Why Not ApolloProvider?

`ApolloProvider` only accepts a single `client` prop. For multiple GraphQL endpoints, we use the `client` option in Apollo hooks instead.

### How It Works

1. **Create clients** using `useApolloClients()` hook
2. **Pass the client** to each `useQuery`, `useMutation`, or `useSubscription`

## Usage Examples

### Basic Query

```typescript
import { useQuery } from '@apollo/client';
import { useApolloClients } from '../hooks/useApolloClients';

function MyComponent() {
  const clients = useApolloClients();
  
  const { data, loading, error } = useQuery(MY_QUERY, {
    client: clients.dashboard,  // Specify which endpoint
  });
  
  return <div>{data?.result}</div>;
}
```

### Query with Options

```typescript
const { data, loading, refetch } = useQuery(GET_KPIS, {
  client: clients.dashboard,
  pollInterval: 30000,           // Poll every 30 seconds
  fetchPolicy: 'cache-and-network',
  variables: { limit: 10 },
});
```

### Mutation

```typescript
import { useMutation } from '@apollo/client';

function CreateProduct() {
  const clients = useApolloClients();
  
  const [createProduct, { loading, error }] = useMutation(CREATE_PRODUCT, {
    client: clients.sales,  // Use sales endpoint
  });
  
  const handleSubmit = async () => {
    await createProduct({ variables: { name: 'Widget' } });
  };
  
  return <button onClick={handleSubmit}>Create</button>;
}
```

### Subscription

```typescript
import { useSubscription } from '@apollo/client';

function LiveOrders() {
  const clients = useApolloClients();
  
  const { data } = useSubscription(ORDER_UPDATES, {
    client: clients.sales,  // WebSocket to sales service
  });
  
  return <div>{data?.orderUpdate?.status}</div>;
}
```

### Multiple Queries in One Component

```typescript
function Dashboard() {
  const clients = useApolloClients();
  
  // Query from dashboard service
  const { data: kpis } = useQuery(GET_KPIS, {
    client: clients.dashboard,
  });
  
  // Query from sales service  
  const { data: orders } = useQuery(GET_RECENT_ORDERS, {
    client: clients.sales,
  });
  
  return (
    <div>
      <KPICards data={kpis} />
      <OrderList data={orders} />
    </div>
  );
}
```

## Available Clients

| Client | GraphQL Endpoint | WebSocket | Use For |
|--------|------------------|-----------|---------|
| `clients.dashboard` | `/dashboard/graphql` | `ws://localhost:5006/dashboard/graphql` | KPIs, Alerts, Metrics |
| `clients.sales` | `/sales/graphql` | `ws://localhost:5004/sales/graphql` | Orders, Invoices, Customers |

### Adding New Clients

1. **Update `useApolloClients` hook**:

```typescript
// frontend/src/hooks/useApolloClients.ts
export function useApolloClients() {
  return useMemo(() => ({
    dashboard: createApolloClient('dashboard'),
    sales: createApolloClient('sales'),
    inventory: createApolloClient('inventory'), // NEW
  }), []);
}
```

2. **Update the interface**:

```typescript
export interface ApolloClients {
  dashboard: ApolloClient<NormalizedCacheObject>;
  sales: ApolloClient<NormalizedCacheObject>;
  inventory: ApolloClient<NormalizedCacheObject>; // NEW
}
```

3. **Use it**:

```typescript
const clients = useApolloClients();
const { data } = useQuery(GET_PRODUCTS, { 
  client: clients.inventory 
});
```

## Alternative Approaches (Not Recommended)

### ❌ Option A: Nested ApolloProviders

```typescript
// Works but verbose and couples components to provider hierarchy
<ApolloProvider client={dashboardClient}>
  <DashboardRoutes />
</ApolloProvider>
<ApolloProvider client={salesClient}>
  <SalesRoutes />
</ApolloProvider>
```

**Downsides**: Can't use multiple clients in one component, couples routing to providers.

### ❌ Option B: Direct Client Method Calls

```typescript
// Works but loses hook benefits (caching, loading states, etc.)
const result = await client.query({ query: MY_QUERY });
```

**Downsides**: No automatic re-renders, manual loading state, no cache integration.

### ❌ Option C: Custom Context

```typescript
// Works but reinvents the wheel
const ClientContext = createContext(clients);
```

**Downsides**: Unnecessary complexity, hooks already support `client` option.

## Best Practices

### ✅ DO

- Use `client` option in hooks for multiple endpoints
- Create clients with `useApolloClients()` hook
- Specify which client each query/mutation uses
- Keep client creation in a single reusable hook

### ❌ DON'T

- Don't use `ApolloProvider` for multiple clients
- Don't create clients in every component (use the hook)
- Don't call `client.query()` directly unless necessary
- Don't mix REST and GraphQL for the same data

## Performance Considerations

### Client Memoization

The `useApolloClients` hook uses `useMemo` to avoid recreating clients on every render:

```typescript
export function useApolloClients() {
  return useMemo(() => ({
    dashboard: createApolloClient('dashboard'),
    sales: createApolloClient('sales'),
  }), []); // Empty deps = created once per component mount
}
```

### Cache Sharing

Each client has its **own cache**. If you need shared data, consider:
- Using the same client for related endpoints
- REST API calls for cross-service data
- Backend aggregation (BFF pattern)

## Troubleshooting

### Error: "Cannot read properties of undefined (reading 'client')"

**Cause**: Forgot to pass `client` option  
**Fix**: Add `client: clients.dashboard` to your hook call

### Error: "Network error: Failed to fetch"

**Cause**: Wrong service name or service not running  
**Fix**: 
1. Check service is running: `http://localhost:5006/graphql`
2. Verify service name matches endpoint: `createApolloClient('dashboard')`

### Queries Not Updating

**Cause**: Different clients don't share cache  
**Fix**: Use same client or call `refetch()` manually

## GraphQL Endpoint URLs

Development:
- Dashboard: `http://localhost:5001/dashboard/graphql`
- Sales: `http://localhost:5001/sales/graphql`
- Dashboard WS: `ws://localhost:5006/dashboard/graphql`
- Sales WS: `ws://localhost:5004/sales/graphql`

Production (via Gateway):
- Dashboard: `https://shopping-now.net/dashboard/graphql`
- Sales: `https://shopping-now.net/sales/graphql`

## Summary

✅ **Recommended Approach**: Use `client` option in Apollo hooks  
✅ **Works with**: `useQuery`, `useMutation`, `useSubscription`, `useLazyQuery`  
✅ **Benefits**: Clean, flexible, supports multiple endpoints per component  
✅ **No Provider Needed**: Apollo hooks work without `ApolloProvider`when `client` is specified  

Example:
```typescript
const clients = useApolloClients();
const { data } = useQuery(GET_KPIS, { client: clients.dashboard });
```
