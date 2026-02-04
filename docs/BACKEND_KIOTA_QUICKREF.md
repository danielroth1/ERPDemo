# Backend Kiota Quick Reference

## Generate Clients

```bash
# All services
dotnet run --project tools/ApiClientGenerator -- --service all

# Specific service
dotnet run --project tools/ApiClientGenerator -- --service inventory
dotnet run --project tools/ApiClientGenerator -- --service sales
dotnet run --project tools/ApiClientGenerator -- --service dashboard

# Check services
dotnet run --project tools/ApiClientGenerator -- --check
```

## VS Code Tasks

- `backend: generate-all-api-clients` - Generate all
- `backend: generate-inventory-api-clients` - Inventory only
- `backend: generate-sales-api-clients` - Sales only
- `backend: generate-dashboard-api-clients` - Dashboard only
- `backend: check-all-service-dependencies` - Check services

## Usage Pattern

```csharp
// 1. Create client
private GeneratedClient CreateClient(string? authToken = null)
{
    var httpClient = _httpClientFactory.CreateClient("ServiceName");
    httpClient.BaseAddress = new Uri(_baseUrl);
    
    if (!string.IsNullOrEmpty(authToken))
        httpClient.DefaultRequestHeaders.Add("Authorization", authToken);

    var authProvider = new AnonymousAuthenticationProvider();
    var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
    adapter.BaseUrl = _baseUrl;

    return new GeneratedClient(adapter);
}

// 2. Use fluent API
var client = CreateClient(authToken);
var response = await client.Api.V1.Resource[id].GetAsync();
return response?.Data?.Value;
```

## Service Ports (VERIFIED)

| Service        | Port |
|----------------|------|
| UserManagement | 5001 |
| Inventory      | 5002 |
| Sales          | 5003 |
| Financial      | 5004 |
| Dashboard      | 5005 |

## Common API Patterns

```csharp
// GET item by ID
await client.Api.V1.Resource[id].GetAsync();

// GET collection
await client.Api.V1.Resources.GetAsync();

// POST create
await client.Api.V1.Resources.PostAsync(request);

// PUT update
await client.Api.V1.Resources[id].PutAsync(request);

// DELETE
await client.Api.V1.Resources[id].DeleteAsync();
```

## Response Handling

```csharp
var response = await client.Api.V1.Resource[id].GetAsync();

if (response?.Success == true && response.Data != null)
{
    // Success
    var data = response.Data;
}
else
{
    // Failure
    _logger.LogWarning("Failed: {Message}", response?.Message);
}
```
