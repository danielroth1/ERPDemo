# Backend Inter-Service Communication with Kiota

## Summary

Successfully implemented **Kiota-generated API clients** for inter-service communication across all backend microservices. This replaces direct HTTP calls with strongly-typed, auto-generated clients.

## ✅ What Was Implemented

### 1. Kiota Packages Added

Added Microsoft.Kiota packages to all services that need inter-service communication:

- `Microsoft.Kiota.Abstractions` (v1.17.3)
- `Microsoft.Kiota.Http.HttpClientLibrary` (v1.17.3)
- `Microsoft.Kiota.Serialization.Json` (v1.17.3)
- `Microsoft.Kiota.Serialization.Text` (v1.17.3)
- `Microsoft.Kiota.Serialization.Form` (v1.17.3)

**Services Updated:**
- ✅ Inventory Management
- ✅ Sales Management
- ✅ Dashboard Analytics

### 2. Generated Kiota Clients

| Consumer Service | Generated Clients | Location |
|-----------------|-------------------|----------|
| **Inventory** | Financial Service | `services/inventory/InventoryManagement/Generated/Clients/Financial/` |
| **Sales** | Inventory Service<br>Financial Service | `services/sales/SalesManagement/Generated/Clients/Inventory/`<br>`services/sales/SalesManagement/Generated/Clients/Financial/` |
| **Dashboard** | User Management<br>Inventory Service<br>Sales Service<br>Financial Service | `services/dashboard/DashboardAnalytics/Generated/Clients/UserManagement/`<br>`services/dashboard/DashboardAnalytics/Generated/Clients/Inventory/`<br>`services/dashboard/DashboardAnalytics/Generated/Clients/Sales/`<br>`services/dashboard/DashboardAnalytics/Generated/Clients/Financial/` |

### 3. Generation Scripts Created

PowerShell scripts for regenerating clients when APIs change:

- ✅ `services/inventory/InventoryManagement/generate-api-clients.ps1`
- ✅ `services/sales/SalesManagement/generate-api-clients.ps1`
- ✅ `services/dashboard/DashboardAnalytics/generate-api-clients.ps1`
- ✅ `scripts/generate-all-backend-api-clients.ps1` (master script)

### 4. VS Code Tasks Added

Added tasks to `.vscode/tasks.json` for easy client regeneration:

**Individual Service Tasks:**
- `backend: generate-inventory-api-clients`
- `backend: generate-sales-api-clients`
- `backend: generate-dashboard-api-clients`

**Dependency Check Tasks:**
- `backend: check-inventory-dependencies`
- `backend: check-sales-dependencies`
- `backend: check-dashboard-dependencies`

**Master Task:**
- `backend: generate-all-api-clients` (generates all at once)

**Usage:** Terminal → Run Task → `backend: generate-inventory-api-clients`

### 5. .gitignore Files Updated

Added to each service's `.gitignore`:
```
# Kiota Generated Files
Generated/
*.kiota.log
kiota-lock.json
```

## 📋 Next Steps

### 1. Update Inventory Service to Use Kiota Client

**Current:** `FinancialServiceClient` uses direct `HttpClient` calls

**Needed:** Replace with Kiota-generated client

**File:** [services/inventory/InventoryManagement/Services/FinancialServiceClient.cs](services/inventory/InventoryManagement/Services/FinancialServiceClient.cs)

**Example Pattern:**
```csharp
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Abstractions.Authentication;
using InventoryManagement.Generated.Clients.Financial;

public class FinancialServiceClient : IFinancialServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    
    public FinancialServiceClient(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _baseUrl = config["Services:Financial"] ?? "http://financial:8080";
    }
    
    private FinancialServiceClient CreateClient()
    {
        var httpClient = _httpClientFactory.CreateClient("FinancialService");
        var authProvider = new AnonymousAuthenticationProvider();
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        adapter.BaseUrl = _baseUrl;
        
        return new FinancialServiceClient(adapter);
    }
    
    public async Task<string?> GetUserAccountIdAsync(string userId, string authToken)
    {
        var client = CreateClient();
        var account = await client.Api.V1.Accounts.User[userId].GetAsync();
        return account?.Data?.Id;
    }
}
```

### 2. Identify Other Inter-Service Calls

**Check Sales Service:** Does it make HTTP calls to Inventory or Financial?
**Check Dashboard Service:** Does it make HTTP calls to other services?

### 3. Test All Services

After updating service implementations:

1. Run all services: `Terminal → Run Task → backend: watch-all-services`
2. Test inter-service communication
3. Verify Kiota clients work correctly

## 🔄 Regenerating Clients

### When to Regenerate

Regenerate clients whenever:
- Backend API endpoints change (new endpoints, changed signatures)
- DTOs/response models are modified
- Route patterns change

### How to Regenerate

**Option 1: VS Code Task (Recommended)**
```
Terminal → Run Task → backend: generate-inventory-api-clients
```

**Option 2: Command Line**
```powershell
cd services/inventory/InventoryManagement
.\generate-api-clients.ps1
```

**Option 3: Regenerate All**
```powershell
.\scripts\generate-all-backend-api-clients.ps1
```

**Note:** Make sure target services are running before generating clients!

## ✅ Benefits

1. **Type Safety**: Compile-time checking for all API calls
2. **Auto-Generated**: No manual HTTP client code needed
3. **Always in Sync**: Regenerate when APIs change
4. **IntelliSense**: Full IDE support with autocomplete
5. **Error Prevention**: Catch API mismatches at build time, not runtime
6. **Maintainability**: Changes to APIs automatically reflected in clients
7. **Documentation**: Generated code documents API structure

## 🚨 Important Notes

1. **Commit generated code** to version control (not ignored)
2. **Do NOT modify generated code** - it will be overwritten
3. **Always regenerate after API changes** in dependent services
4. **Use wrapper interfaces** for testability (e.g., `IFinancialServiceClient`)
5. **Set baseUrl at runtime** - Kiota clients need manual baseUrl configuration

## 📚 Resources

- [Microsoft Kiota Documentation](https://learn.microsoft.com/en-us/openapi/kiota/)
- [Kiota C# Guide](https://learn.microsoft.com/en-us/openapi/kiota/quickstarts/dotnet)
- [OpenAPI Specification](https://swagger.io/specification/)

---

**Status:** ✅ Generation Complete | ⚠️ Implementation In Progress

**Last Updated:** February 2, 2026
