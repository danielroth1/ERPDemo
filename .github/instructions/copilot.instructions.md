# Contributing to ERP System

Welcome to the ERP System project! This guide will help you understand the project structure and how to work with it effectively.

## 📁 Project Structure

```
erp/
├── .github/                      # GitHub workflows and templates
│   ├── workflows/                # CI/CD pipelines
│   └── CONTRIBUTING.md           # This file
├── .vscode/                      # VS Code configuration
│   ├── launch.json               # Debug configurations
│   └── tasks.json                # Build and run tasks
├── services/                     # Backend microservices
│   ├── gateway/                  # API Gateway (YARP)
│   │   ├── ApiGateway/           # Gateway service project
│   │   └── README.md             # Gateway documentation
│   ├── user-management/          # User & Authentication service
│   │   ├── UserManagement/       # Service project
│   │   ├── UserManagement.Tests/ # Unit tests
│   │   └── README.md             # Service documentation
│   ├── inventory-management/     # Inventory service
│   │   ├── InventoryManagement/  # Service project
│   │   ├── InventoryManagement.Tests/
│   │   └── README.md
│   ├── sales-orders/             # Sales & Orders service
│   │   ├── SalesOrders/          # Service project
│   │   └── README.md
│   ├── financial-management/     # Financial service
│   │   ├── FinancialManagement/  # Service project
│   │   └── README.md
│   └── dashboard-analytics/      # Dashboard & Analytics service
│       ├── DashboardAnalytics/   # Service project
│       └── README.md
├── frontend/                     # React application
│   ├── src/
│   │   ├── components/           # Reusable UI components
│   │   ├── features/             # Feature modules
│   │   │   ├── auth/             # Authentication
│   │   │   ├── inventory/        # Inventory management
│   │   │   ├── users/            # User management
│   │   │   ├── sales/            # Sales & orders
│   │   │   ├── financial/        # Financial management
│   │   │   └── analytics/        # Analytics & reporting
│   │   ├── store/                # Redux store
│   │   ├── services/             # API services
│   │   └── App.tsx               # Root component
│   ├── package.json
│   └── vite.config.ts
├── infrastructure/               # Docker & Kubernetes configs
│   ├── docker-compose.yml        # Local development stack
│   ├── kubernetes/               # K8s manifests
│   └── monitoring/               # Prometheus, Grafana configs
├── docs/                         # Documentation
│   ├── ARCHITECTURE.md
│   ├── DEPLOYMENT.md
│   └── API_DOCUMENTATION.md
├── DEPLOYMENT.md                 # Production deployment guide
├── FINAL_STATUS.md               # Project completion status
├── PROJECT_SUMMARY.md            # Comprehensive overview
└── README.md                     # Main documentation

```

## 🚀 Getting Started

### Prerequisites

- **Windows 10/11** with PowerShell 7+
- **Docker Desktop** for Windows
- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 20+** with npm - [Download](https://nodejs.org/)
- **Visual Studio Code** - [Download](https://code.visualstudio.com/)
- **Git** for version control

### Initial Setup

1. **Clone the repository**
   ```powershell
   git clone <repository-url>
   cd erp
   ```

2. **Start infrastructure** (MongoDB, Kafka, Prometheus, Grafana)
   ```powershell
   cd infrastructure
   docker-compose up -d
   cd ..
   ```

3. **Install frontend dependencies**
   ```powershell
   cd frontend
   npm install
   cd ..
   ```

4. **Open in VS Code**
   ```powershell
   code .
   ```

## 🔧 Development Workflow

### Using VS Code Tasks

The project includes pre-configured VS Code tasks for common operations:

#### Start All Services (Recommended)
1. **Terminal > Run Task** → `docker-compose-up` (infrastructure)
2. **Terminal > Run Task** → `watch-all-services` (all backend services with hot reload)
3. **Terminal > Run Task** → `dev-frontend` (React dev server)

#### Individual Service Tasks
- `watch-gateway` - Run API Gateway with hot reload
- `watch-user-management` - Run User Management with hot reload
- `watch-inventory-management` - Run Inventory with hot reload
- `watch-sales-orders` - Run Sales with hot reload
- `watch-financial-management` - Run Financial with hot reload
- `watch-dashboard-analytics` - Run Dashboard with hot reload

#### Build Tasks
- `build-all-services` - Build all backend services
- `build-frontend` - Build production frontend bundle

#### Test Tasks
- `test-all` - Run all tests
- `test-user-management` - Run User Management tests
- `test-inventory-management` - Run Inventory tests

### Using VS Code Debugger

#### Debug All Services
1. **Run > Start Debugging** (F5)
2. Select **"Launch All Backend Services"**
3. Set breakpoints in any service
4. Services will stop at breakpoints automatically

#### Debug Individual Service
1. Set breakpoints in the service code
2. **Run > Start Debugging** (F5)
3. Select **"Launch API Gateway"** (or any service)
4. Swagger opens automatically at the service endpoint

#### Attach to Running Services
If services are already running via `dotnet watch run`:
1. **Run > Start Debugging** (F5)
2. Select **"Attach to All Services"**
3. Debugger attaches to all running processes

### Manual Service Commands

If you prefer command line:

```powershell
# Start API Gateway
cd services/gateway/ApiGateway
dotnet watch run

# Start User Management
cd services/user-management/UserManagement
dotnet watch run

# Start Frontend
cd frontend
npm run dev
```

## 🏗️ Architecture Overview

### Backend Services

Each microservice follows this structure:

```
ServiceName/
├── Controllers/          # REST API endpoints
├── Services/             # Business logic
├── Models/               # Domain entities
├── Data/                 # Database context
├── Events/               # Kafka event models
├── GraphQL/              # GraphQL schemas (Sales, Dashboard only)
├── Hubs/                 # SignalR hubs (Dashboard only)
├── Program.cs            # Application entry point
└── appsettings.json      # Configuration

ServiceName.Tests/
├── Models/               # Model/entity tests
├── Services/             # Service tests
└── Controllers/          # Controller tests
```

### Service Ports

| Service | Port | URL |
|---------|------|-----|
| API Gateway | 5000 | http://localhost:5000 |
| User Management | 5001 | http://localhost:5001 |
| Inventory Management | 5002 | http://localhost:5002 |
| Sales & Orders | 5003 | http://localhost:5003 |
| Financial Management | 5004 | http://localhost:5004 |
| Dashboard & Analytics | 5005 | http://localhost:5005 |
| Frontend (Dev) | 5173 | http://localhost:5173 |

### Frontend Structure

```
src/
├── components/           # Shared components (Modal, LoadingSpinner, etc.)
├── features/             # Feature modules
│   ├── auth/             # Login, Register pages
│   ├── inventory/        # Products, Categories, Stock
│   ├── users/            # User management, Roles
│   ├── sales/            # Orders, Customers, Invoices
│   ├── financial/        # Accounts, Transactions, Reports
│   └── analytics/        # Dashboard, KPIs, Alerts
├── store/                # Redux store and slices
├── services/             # API service layer
├── App.tsx               # Root component with routing
└── main.tsx              # Application entry point
```

## 🧪 Testing

### Backend Tests

```powershell
# Run all tests across all services
dotnet test erp.sln --verbosity minimal

# Run specific service tests
dotnet test services/user-management/UserManagement.Tests/
dotnet test services/inventory/InventoryManagement.Tests/
dotnet test services/sales/SalesManagement.Tests/
dotnet test services/financial/FinancialManagement.Tests/
dotnet test services/dashboard/DashboardAnalytics.Tests/
dotnet test services/orchestration/Orchestration.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Frontend Tests

```powershell
cd frontend

# Run unit tests (when implemented)
npm test

# Run E2E tests (when implemented)
npm run test:e2e
```

### Test Project Structure

Test projects are **co-located** with their service (not in a centralized `tests/` folder):

```
services/
├── user-management/
│   ├── UserManagement/              # Service project
│   └── UserManagement.Tests/        # Test project (net9.0)
│       ├── Helpers/
│       │   └── DbContextHelper.cs   # InMemory DB factory
│       ├── Models/
│       │   ├── UserModelTests.cs    # Entity default values, properties
│       │   ├── RoleTests.cs         # Enum values
│       │   └── DtoTests.cs          # DTO/response model tests
│       └── Services/
│           ├── UserServiceTests.cs  # CRUD, events, pagination
│           └── JwtServiceTests.cs   # Token generation, validation, refresh
├── inventory/
│   ├── InventoryManagement/
│   └── InventoryManagement.Tests/
│       ├── Helpers/DbContextHelper.cs
│       └── Services/
│           ├── ProductServiceTests.cs
│           ├── CategoryServiceTests.cs
│           └── StockMovementServiceTests.cs
```

### Test Framework & Packages

All test projects use:
- **xUnit 2.9.2** - Test framework
- **Moq 4.20.72** - Mocking framework
- **FluentAssertions 6.12.1** - Assertion library
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database provider
- **coverlet** - Code coverage

### Writing Tests: Key Patterns

#### 1. InMemory DbContext Helper

Each test project has a `Helpers/DbContextHelper.cs` that creates an isolated InMemory database per test:

```csharp
public static class DbContextHelper
{
    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new TestAppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    // Subclass to configure JSONB value objects for InMemory provider
    private class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure OwnsOne/OwnsMany for JSONB value objects
            // e.g., modelBuilder.Entity<Customer>().OwnsOne(c => c.DefaultBillingAddress);
        }
    }
}
```

#### 2. Service Test Pattern

Services use `IDisposable` with real InMemory DbContext and mocked Kafka producers:

```csharp
public class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITopicProducer<ProductCreated>> _eventProducer;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _eventProducer = new Mock<ITopicProducer<ProductCreated>>();
        _service = new ProductService(_dbContext, _eventProducer.Object, ...);
    }

    public void Dispose() => _dbContext.Dispose();

    [Fact]
    public async Task CreateAsync_ShouldPublishEvent()
    {
        var product = CreateProduct();
        await _service.CreateAsync(product);

        _eventProducer.Verify(p => p.Produce(
            It.Is<ProductCreated>(e => e.ProductId == product.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

#### 3. What to Test

- **Service methods**: All CRUD operations, edge cases (not found, duplicates), pagination
- **Domain events**: Verify Kafka events are published with correct data via `Mock.Verify()`
- **Model defaults**: Entity default values, computed properties (e.g., `AvailableQuantity`)
- **DTOs**: Response mapping, factory methods (`ApiResponse.SuccessResponse()`, `ErrorResponse()`)
- **Business logic**: Stock calculations, role checks, email uniqueness

#### 4. Naming Convention

```
MethodName_Scenario_ExpectedBehavior
```

Examples:
- `GetByIdAsync_WithExistingId_ShouldReturnProduct`
- `CreateAsync_ShouldPublishProductCreatedEvent`
- `DeleteAsync_WithNonExistingId_ShouldReturnFalse`

### Test Structure Categories

- **Model Tests**: Validate entity properties, defaults, computed properties
- **Service Tests**: Test business logic with InMemory DB + mocked Kafka producers
- **Integration Tests**: Test with real database (TestContainers)
- **E2E Tests**: Full user workflows (Playwright)

## 📝 Code Style & Conventions

### C# Backend

- **Naming**: PascalCase for classes, methods, properties
- **Controllers**: Suffix with `Controller` (e.g., `ProductsController`)
- **Services**: Suffix with `Service` (e.g., `ProductService`)
- **Async methods**: Suffix with `Async` (e.g., `GetProductAsync`)
- **Dependency Injection**: Use constructor injection
- **Error Handling**: Use appropriate HTTP status codes

Example:
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(string id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound();
        return Ok(product);
    }
}
```

### TypeScript Frontend

- **Naming**: camelCase for variables/functions, PascalCase for components
- **Components**: Functional components with TypeScript
- **State**: Redux Toolkit with typed slices
- **API calls**: **MUST use Kiota-generated clients** - Never use direct fetch/axios calls
- **Types**: Use types from generated Kiota models
- **Admin role check**: Use `user.roles?.some(role => role === 2)` to check for admin. Roles: `0` = User, `1` = Manager, `2` = Admin.

### API Gateway Configuration

- **New Endpoints**: When adding new controller endpoints to any service, **MUST** update `services/gateway/ApiGateway/appsettings.json` to add routing configuration
- Add routes to the `ReverseProxy.Routes` section with appropriate cluster mapping
- Configure `AuthorizationPolicy` if authentication is required
- Example:
  ```json
  "new-endpoint-route": {
    "ClusterId": "service-cluster",
    "AuthorizationPolicy": "authenticated",
    "Match": {
      "Path": "/api/v1/endpoint/{**catch-all}"
    }
  }
  ```

### Service-to-Service Communication

- **REST Communication**: Use clean, typed abstraction layers over direct HTTP calls
- Create dedicated service client classes (e.g., `FinancialServiceClient`, `InventoryServiceClient`)
- Implement interface-based design for testability (e.g., `IFinancialServiceClient`)
- Use strongly-typed DTOs instead of anonymous objects
- Register as typed `HttpClient` using `AddHttpClient<TClient, TImplementation>()`
- Benefits: type safety, centralized error handling, better maintainability, easier testing
- Example:
  ```csharp
  // Service client interface
  public interface IFinancialServiceClient
  {
      Task<AccountResponse?> GetAccountAsync(string accountId);
      Task<bool> CreateTransactionAsync(CreateTransactionRequest request);
  }
  
  // Registration in Program.cs
  builder.Services.AddHttpClient<IFinancialServiceClient, FinancialServiceClient>();
  ```
- **API Generation**: When backend APIs change or new endpoints are added:
  1. Navigate to the frontend folder
  2. Run the VS Code task `frontend: generate-all-api-clients` to regenerate all Kiota clients
  3. **IMPORTANT**: Test that the frontend still builds:
     - If a watch task is running (e.g., `dev-frontend`), observe for compilation errors
     - Otherwise, run `npm run build` to verify the build succeeds
     - Fix any TypeScript errors that arise from API changes
  4. Always use the generated clients from `frontend/src/generated/clients/` instead of creating custom API calls
  5. If Kiota reports OpenAPI errors (e.g., invalid schema keys), fix the backend DTOs/responses causing the issue
  6. **NEVER manually edit files inside `frontend/src/generated/clients/`** — they are fully overwritten on every generation. To add a new endpoint, add it to the backend service, ensure Swagger is enabled on that service, add the service to `kiota-config.json` if it is new, then re-run `frontend: generate-all-api-clients`.

#### 🚨 MANDATORY: Kiota API Client Usage

**ALL REST API calls MUST use Kiota-generated TypeScript clients.**

❌ **NEVER DO THIS**:
```typescript
// DON'T use direct fetch or axios
const response = await fetch('/api/v1/products');
const response = await axios.get('/api/v1/products');
const response = await apiService.get('/products');
```

✅ **ALWAYS DO THIS**:
```typescript
// DO use Kiota-generated clients
import { createInventoryClient } from '../generated/clients/inventory/inventoryClient';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { BearerTokenAuthenticationProvider } from './auth/bearer-token-provider';

class InventoryService {
  private client;

  constructor() {
    const authProvider = new BearerTokenAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000';
    this.client = createInventoryClient(adapter);
  }

  async getProducts() {
    return await this.client.api.v1.products.get();
  }
}
```

#### ⚠️ MANDATORY: Regenerating API Clients After Backend Changes

**YOU MUST regenerate API clients after ANY backend API change:**
- ✅ After adding new controllers or endpoints to backend
- ✅ After modifying existing endpoint signatures
- ✅ After adding new DTOs or response models
- ✅ After changing route patterns or HTTP methods
- ✅ After modifying OpenAPI/Swagger annotations

**ALWAYS run this task after backend changes:**

**Via VS Code Task (Recommended)**:
1. Terminal → Run Task → `generate-api-clients` (all services)
2. Or Terminal → Run Task → `generate-api-client-dashboard` (specific service)
3. Or Terminal → Run Task → `check-services` (verify services are running)

**Via PowerShell**:
```powershell
# Make sure all services are running first
.\scripts\generate-api-clients.ps1

# Or regenerate specific service
.\scripts\generate-api-clients.ps1 -Service dashboard

# Check which services are running
.\scripts\generate-api-clients.ps1 -CheckServices
```

**⚠️ If you forget to regenerate**: Frontend will have TypeScript errors or runtime API call failures.

**Client locations**:
- Dashboard: `frontend/src/generated/clients/dashboard/`
- User Management: `frontend/src/generated/clients/user-management/`
- Inventory: `frontend/src/generated/clients/inventory/`
- Sales: `frontend/src/generated/clients/sales/`
- Financial: `frontend/src/generated/clients/financial/`
- Orchestration: `frontend/src/generated/clients/orchestration/`

Example:
```typescript
interface Product {
  id: string;
  name: string;
  price: number;
  stockQuantity: number;
}

const ProductList: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);

  useEffect(() => {
    fetchProducts();
  }, []);

  const fetchProducts = async () => {
    try {
      // Use Kiota-generated client (inventoryService wraps the client)
      const response = await inventoryService.getProducts();
      setProducts(response.data);
    } catch (error) {
      console.error('Failed to fetch products:', error);
    }
  };

  return (
    <div>
      {products.map(product => (
        <div key={product.id}>{product.name}</div>
      ))}
    </div>
  );
};
```

## 🔀 Git Workflow

### Branch Strategy

- **`main`** - Production-ready code
- **`develop`** - Integration branch for features
- **`feature/<name>`** - New features
- **`fix/<name>`** - Bug fixes
- **`test/<name>`** - Test implementations

### Commit Messages

Use conventional commit format:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Build process or tooling changes

**Examples**:
```
feat(inventory): add low-stock alert functionality

Added automatic alerts when product stock falls below reorder level.
Integrated with Kafka event bus for real-time notifications.

Closes #123

---

fix(auth): resolve JWT token expiration issue

Token refresh was not working correctly due to timezone mismatch.
Updated token validation to use UTC timestamps.

---

test(user-management): add model tests for User entity

Added comprehensive tests for User model including:
- Default values validation
- Role assignment
- Email normalization
```

### Pull Request Process

1. **Create feature branch**
   ```powershell
   git checkout -b feature/my-feature
   ```

2. **Make changes and commit**
   ```powershell
   git add .
   git commit -m "feat(scope): description"
   ```

3. **Run the frontend linter before pushing**
   ```bash
   cd frontend && npm run lint
   ```
   Fix all errors (not just warnings) before continuing. The CI pipeline will fail on lint errors.

4. **Push to remote**
   ```powershell
   git push origin feature/my-feature
   ```

4. **Create Pull Request** on GitHub
   - Fill out PR template
   - Link related issues
   - Request reviews

5. **Address review comments**

6. **Merge** after approval (squash or rebase)

## 🐛 Debugging Tips

### Using Grafana for Multi-Service Debugging

The ERP system uses a full observability stack: **Grafana** (UI) + **Prometheus** (metrics) + **Loki** (logs) + **Grafana Alloy** (log collector) + **Grafana Tempo** (distributed tracing).

#### Local Development

| Tool | URL | Credentials |
|------|-----|-------------|
| **Grafana** | http://localhost:3001 | admin / admin |
| **Prometheus** | http://localhost:9090 | — |
| **Loki** | http://localhost:3100 | — |
| **Tempo** | http://localhost:3200 | — |
| **Alloy UI** | http://localhost:12345 | — |
| **Kafka UI** | http://localhost:9000 | — |

Start the infra stack: `docker compose -f infrastructure/docker-compose.dev.yml up -d`

#### How to Debug a Failing Request in Grafana

1. Open **Grafana → Explore** (compass icon)
2. Select **Loki** datasource
3. Query by service: `{service="gateway"}` or `{service="inventory"}`
4. Filter for errors: `{service=~".+"} |~ "(?i)(error|exception|fail)"`
5. Click a log line → view its trace via structured JSON fields
6. Switch to **Prometheus** datasource to correlate with `http_requests_received_total` metric

The **ERP Overview** dashboard (Dashboards → ERP folder) provides:
- HTTP request rate per service
- 5xx error rate
- P95 / P50 latency
- Service UP/DOWN status
- Live log viewer (searchable)

#### Production

Grafana is available at **https://monitoring.shopping-now.net** (requires DNS `monitoring.shopping-now.net → <k3s-server-ip>`).

Before first deploy, create the Grafana admin secret on the cluster:
```bash
KUBECONFIG=~/.kube/k3s-erp.yaml kubectl create secret generic grafana-admin-secret \
  --from-literal=admin-user=admin \
  --from-literal=admin-password='STRONG_PASSWORD_HERE' \
  -n erp-prod
```

### Observability Architecture

```
.NET services (host/pod)
    │
    ├── /metrics  ──────────────────► Prometheus (scrapes every 15s)
    │                                          │
    │                                          ▼
    │                                       Grafana
    │                                          ▲
    ├── Serilog GrafanaLoki sink ─► Loki  ────┘
    │                                ▲         │
    │                    Grafana Alloy          │ (trace→log correlation)
    │                    (Docker/K8s logs)      │
    │                                          ▼
    └── OpenTelemetry OTLP ────────► Tempo ───┘
                                   (distributed tracing)
```

- **.NET services** push logs directly to Loki via `Serilog.Sinks.Grafana.Loki`
- **Grafana Alloy** collects logs from infrastructure containers (postgres, kafka, redis) and all K8s pods
- **Prometheus** scrapes `/metrics` from all services
- **Tempo** receives OTLP traces from all services via gRPC (port 4317)
- **OpenTelemetry** auto-instruments ASP.NET Core, HttpClient, and MassTransit

### MassTransit Saga Architecture

The ERP system uses **MassTransit** with **Kafka transport** (Rider) for workflow orchestration via sagas.

#### Shared Contracts
All message types are defined in `services/shared/ERP.Contracts/`:
- **Commands**: `SubmitPurchase`, `SubmitReturn`, `ReserveStock`, `DeductStock`, `RestoreStock`, `CreatePurchaseTransaction`, `CreateRefundTransaction`
- **Saga Events**: `StockReserved`, `StockReservationFailed`, `PurchaseTransactionCreated`, etc.
- **Domain Events**: `UserCreated`, `ProductUpdated`, `OrderCreated`, `TransactionCreated`, etc.
- **KafkaTopics**: Static constants for all topic names

#### Purchase Saga Flow
```
Gateway: SubmitPurchase → [ReserveStock] → Inventory
Inventory: StockReserved → Gateway
Gateway: [CreatePurchaseTransaction] → Financial
Financial: PurchaseTransactionCreated → Gateway
Gateway: [DeductStock] → Inventory
Inventory: StockDeducted → Gateway
Gateway: PurchaseCompleted → HTTP Response
```
Compensation: If financial transaction fails, Gateway sends `RestoreStock` to Inventory.

#### Return Saga Flow
```
Gateway: SubmitReturn → [CreateRefundTransaction] → Financial
Financial: RefundTransactionCreated → Gateway
Gateway: [RestoreStock] → Inventory
Inventory: StockRestored → Gateway
Gateway: ReturnCompleted → HTTP Response
```

#### Service Roles
| Service | Role | Key Files |
|---------|------|-----------|
| **Gateway** | Saga orchestrator + HTTP bridge | `Sagas/PurchaseStateMachine.cs`, `Services/PurchaseTracker.cs`, `Controllers/ShopController.cs` |
| **Inventory** | Command consumer (reserve/deduct/restore stock) | `Consumers/ReserveStockConsumer.cs`, etc. |
| **Financial** | Command consumer (create transactions) | `Consumers/CreatePurchaseTransactionConsumer.cs`, etc. |
| **Dashboard** | Domain event consumer (analytics) | `Consumers/UserEventConsumers.cs`, etc. |
| **UserMgmt** | Domain event producer | Uses `ITopicProducer<T>` |
| **Sales** | Domain event producer | Uses `ITopicProducer<T>` |

#### Adding a New MassTransit Consumer
1. Define message types in `services/shared/ERP.Contracts/`
2. Add topic name to `KafkaTopics.cs` if new
3. Create consumer class implementing `IConsumer<T>`
4. Register in `Program.cs`: `rider.AddConsumer<T>()` + `k.TopicEndpoint<TMessage>(...)`
5. Add producer if the consumer publishes events: `rider.AddProducer<TEvent>(topic)`

#### Adding a New Service to the Observability Stack

1. Add to `.csproj`: `<PackageReference Include="Serilog.Sinks.Grafana.Loki" Version="8.3.0" />`
2. Add to `appsettings.Development.json` Serilog WriteTo:
   ```json
   { "Name": "GrafanaLoki", "Args": { "uri": "http://localhost:3100", "labels": [{ "key": "service", "value": "YOUR-SERVICE-NAME" }] } }
   ```
3. Add a Prometheus scrape target to `infrastructure/monitoring/prometheus/prometheus.yml` (local) and `infrastructure/k8s/base/prometheus.yaml` (K8s ConfigMap)

### Backend Services

1. **Check service health**: `http://localhost:500X/health/ready`
2. **View Swagger docs**: `http://localhost:500X/swagger`
3. **Check logs**: Services log to console in JSON format (also shipped to Loki in development)
4. **Kafka**: Access Kafka UI at `http://localhost:9000`

### Frontend

1. **Redux DevTools**: Install extension to inspect state
2. **Network tab**: Monitor API calls in browser DevTools
3. **React DevTools**: Install extension to inspect components
4. **Console logs**: Check for errors and warnings

### Common Issues

| Issue | Solution |
|-------|----------|
| Service won't start | Check postgres/kafka are running (`docker compose -f infrastructure/docker-compose.dev.yml ps`) |
| 401 Unauthorized | Verify JWT token is valid and not expired |
| CORS errors | Ensure frontend origin is in Gateway CORS policy |
| Database connection failed | Check connection string in `appsettings.json` |
| Port already in use | Change port in `appsettings.json` or kill process |
| Prometheus targets DOWN | Check service is running; verify port in `prometheus.yml` matches `launchSettings.json` |
| Logs not in Loki | Ensure `appsettings.Development.json` has GrafanaLoki sink; check `ASPNETCORE_ENVIRONMENT=Development` |

## 🔧 Adding a New Service

When creating a new microservice, follow these steps to integrate it properly:

### 1. Create Service Structure
```
services/
└── your-service/
    ├── YourService/              # Main project
    │   ├── Controllers/
    │   ├── Models/
    │   ├── Services/
    │   ├── Program.cs
    │   └── YourService.csproj
    ├── YourService.Tests/        # Unit tests
    └── README.md                 # Service documentation
```

### 2. Update VS Code Configuration

#### Add to `.vscode/tasks.json`:

**Watch Task** (for development with hot reload):
```json
{
  "label": "watch-your-service",
  "command": "dotnet",
  "type": "process",
  "args": [
    "watch",
    "run",
    "--project",
    "${workspaceFolder}/services/your-service/YourService/YourService.csproj"
  ],
  "problemMatcher": "$msCompile",
  "isBackground": true,
  "presentation": {
    "reveal": "always",
    "panel": "dedicated",
    "group": "backend"
  },
  "options": {
    "cwd": "${workspaceFolder}/services/your-service/YourService",
    "env": {
      "ASPNETCORE_ENVIRONMENT": "Development",
      "ASPNETCORE_URLS": "http://localhost:500X"  // Use next available port
    }
  }
}
```

**Build Task**:
```json
{
  "label": "build-your-service",
  "command": "dotnet",
  "type": "process",
  "args": [
    "build",
    "${workspaceFolder}/services/your-service/YourService/YourService.csproj",
    "/property:GenerateFullPaths=true",
    "/consoleloggerparameters:NoSummary"
  ],
  "problemMatcher": "$msCompile",
  "group": "build"
}
```

**Test Task**:
```json
{
  "label": "test-your-service",
  "command": "dotnet",
  "type": "process",
  "args": [
    "test",
    "${workspaceFolder}/services/your-service/YourService.Tests/YourService.Tests.csproj",
    "--verbosity",
    "minimal"
  ],
  "problemMatcher": "$msCompile",
  "group": "test"
}
```

**Update `watch-all-services` task** to include your new service:
```json
{
  "label": "watch-all-services",
  "dependsOn": [
    "watch-gateway",
    "watch-user-management",
    "watch-inventory",
    "watch-sales",
    "watch-financial",
    "watch-dashboard",
    "watch-your-service"  // ADD THIS LINE
  ]
}
```

**Update `build-all-services` task** to include your new service:
```json
{
  "label": "build-all-services",
  "dependsOn": [
    "build-gateway",
    "build-user-management",
    "build-inventory",
    "build-sales",
    "build-financial",
    "build-dashboard",
    "build-your-service"  // ADD THIS LINE
  ]
}
```

#### Add to `.vscode/launch.json`:

**Launch Configuration**:
```json
{
  "name": "Launch Your Service",
  "type": "coreclr",
  "request": "launch",
  "preLaunchTask": "build-your-service",
  "program": "${workspaceFolder}/services/your-service/YourService/bin/Debug/net8.0/YourService.dll",
  "args": [],
  "cwd": "${workspaceFolder}/services/your-service/YourService",
  "stopAtEntry": false,
  "serverReadyAction": {
    "action": "openExternally",
    "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
    "uriFormat": "%s/swagger"
  },
  "env": {
    "ASPNETCORE_ENVIRONMENT": "Development",
    "ASPNETCORE_URLS": "http://localhost:500X"  // Match the port from watch task
  },
  "sourceFileMap": {
    "/Views": "${workspaceFolder}/Views"
  }
}
```

**Attach Configuration**:
```json
{
  "name": "Attach to Your Service",
  "type": "coreclr",
  "request": "attach",
  "processName": "YourService"
}
```

### 3. Update Infrastructure

#### Add to `docker-compose.yml`:
```yaml
your-service:
  build:
    context: ./services/your-service
    dockerfile: Dockerfile
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - MongoDb__ConnectionString=mongodb://mongodb:27017
    - MongoDb__DatabaseName=erp_yourservice
    - Kafka__BootstrapServers=kafka:9092
  ports:
    - "500X:80"
  depends_on:
    - mongodb
    - kafka
  networks:
    - erp-network
```

#### ⚠️ MANDATORY: Keep infrastructure files in sync

When adding a new service or any infrastructure resource (databases, message brokers, storage, etc.), you **MUST** update ALL THREE of these locations to keep them consistent:

1. **`docker-compose.yml`** — production/full-stack compose file (root of project)
2. **`infrastructure/docker-compose.dev.yml`** — dev infra-only compose file (used for local development; does NOT include application services)
3. **`infrastructure/k8s/`** — Kubernetes manifests (Deployment + Service YAML for each resource)

**Environment variable rules**:
- Secrets and environment-specific values **MUST** be stored in `.env` (root) and referenced in compose files as `${VAR_NAME}`
- Development-specific configuration (URLs, ports, feature flags) goes in `appsettings.Development.json` of the relevant service, **not** hardcoded in compose files
- Never commit `.env` files with real secrets

#### Add to API Gateway routes (if needed):
Update `services/gateway/ApiGateway/appsettings.json`:
```json
{
  "ReverseProxy": {
    "Routes": {
      "your-service-route": {
        "ClusterId": "your-service-cluster",
        "Match": {
          "Path": "/api/yourservice/{**catch-all}"
        },
        "Transforms": [
          { "PathPattern": "/api/{**catch-all}" }
        ]
      }
    },
    "Clusters": {
      "your-service-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://your-service:80"
          }
        }
      }
    }
  }
}
```

### 4. Update Documentation

- [ ] Add service description to main `README.md`
- [ ] Create service-specific `README.md` with API endpoints
- [ ] Update `docs/ARCHITECTURE.md` with service responsibilities
- [ ] Add service to deployment documentation
- [ ] Update this checklist if needed

### 5. Checklist for New Service Integration

- [ ] Service created with proper structure
- [ ] `watch-{service}` task added to `.vscode/tasks.json`
- [ ] `build-{service}` task added to `.vscode/tasks.json`
- [ ] `test-{service}` task added to `.vscode/tasks.json`
- [ ] Service added to `watch-all-services` dependencies
- [ ] Service added to `build-all-services` dependencies
- [ ] Launch configuration added to `.vscode/launch.json`
- [ ] Attach configuration added to `.vscode/launch.json`
- [ ] Docker service added to `docker-compose.yml`
- [ ] Gateway route configured (if needed)
- [ ] MongoDB database configured
- [ ] Kafka integration configured (if needed)
- [ ] Service README created
- [ ] API documentation added
- [ ] Unit tests project created
- [ ] Health checks implemented
- [ ] Swagger/OpenAPI configured
- [ ] Logging configured (Serilog)
- [ ] Metrics endpoint added (Prometheus)
- [ ] `backend: generate-{service}-api-clients` VS Code task added to `.vscode/tasks.json` and added as dependency to `generate-all-api-clients`
- [ ] **Kiota API client generated** (run VS Code task `backend: generate-{service}-api-clients`)
- [ ] **Frontend service wrapper created** using Kiota client (never direct API calls)

## 📚 Additional Resources

- **Project Documentation**: See `/docs` folder
- **API Documentation**: Swagger UI at each service
- **Deployment Guide**: `DEPLOYMENT.md`
- **Architecture Details**: `docs/ARCHITECTURE.md`
- **Service READMEs**: Each service has its own README

## 🚨 Production Server Management

### Server Details
- **Domain**: shopping-now.net
- **Server**: <contabo-server> (Contabo VPS)
- **SSH User**: daniel
- **Project Location**: `/home/daniel/ERPDemo`

### Connecting to Production Server

```bash
# SSH into the server
ssh ${DEPLOY_SERVER}

# Navigate to project directory
cd /home/daniel/ERPDemo

# Check container status
docker-compose ps

# View logs
docker-compose logs -f [service-name]
docker-compose logs --tail=100 gateway
docker-compose logs --tail=100 user-management
```

### Common Production Issues & Solutions

#### 1. Containers Not Running
```bash
# Check all containers
docker-compose ps

# Restart specific service
docker-compose restart [service-name]

# Restart all services
docker-compose restart

# View recent logs
docker-compose logs --tail=50 [service-name]
```

#### 2. 401 Unauthorized Errors (JWT Token Issues)
**Cause**: JWT secret mismatch between services

**Solution**:
```bash
# Verify JWT secrets match across all services
docker exec erp-gateway printenv | grep Jwt
docker exec erp-user-management printenv | grep Jwt
docker exec erp-dashboard printenv | grep Jwt

# If secrets don't match, check .env file
cat .env | grep JWT

# Redeploy services with correct configuration
docker-compose up -d --force-recreate gateway user-management dashboard
```

**Critical**: All services (gateway, user-management, inventory, sales, financial, dashboard) must have the same `Jwt__Secret`, `Jwt__Issuer`, and `Jwt__Audience`.

#### 3. 502 Bad Gateway Errors
**Cause**: Gateway can't reach backend services

**Solution**:
```bash
# Check if services are running
docker-compose ps

# Check gateway logs
docker-compose logs --tail=50 gateway

# Common issue: Gateway using localhost instead of container names
# Services must use container names: user-management:8080, not localhost:5001

# Verify gateway configuration
docker exec erp-gateway cat /app/appsettings.Production.json

# Restart gateway
docker-compose restart gateway
```

#### 4. CORS Errors
**Cause**: Gateway CORS policy doesn't include the frontend domain

**Solution**: Gateway CORS must allow `https://shopping-now.net` and development ports (`localhost:5173`, `localhost:5174`)

#### 5. Frontend Not Loading / 502 on Root URL
**Cause**: Frontend container not running or nginx misconfigured

**Solution**:
```bash
# Check frontend container
docker-compose ps frontend
docker-compose logs --tail=50 frontend

# Restart frontend
docker-compose restart frontend

# Check nginx reverse proxy
sudo nginx -t
sudo systemctl status nginx
sudo tail -f /var/log/nginx/error.log
```

### Production Deployment Process

#### From Local Machine:
```bash
# Deploy all services
./deploy.sh deploy

# Deploy specific services only
./deploy.sh deploy --services "frontend gateway"

# View logs
./deploy.sh logs --services "gateway"

# Check status
./deploy.sh check

# SSH to server
./deploy.sh ssh
```

#### On Production Server:
```bash
cd /home/daniel/ERPDemo

# Pull latest changes (if needed)
# git pull origin main

# Rebuild and restart services
docker-compose up -d --build

# View status
docker-compose ps

# View logs
docker-compose logs -f
```

### Critical Production Configuration

#### Environment Variables (.env file)
Located at: `/home/daniel/ERPDemo/.env`

**Must be configured**:
```bash
# JWT Configuration (CRITICAL - must match across all services)
JWT_SECRET=your-secret-key-min-32-characters-long-for-security-change-this
JWT_ISSUER=erp-system
JWT_AUDIENCE=erp-clients

# SMTP Configuration (for user registration emails)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_FROM_EMAIL=noreply@shopping-now.net
```

**Note**: `docker-compose.override.yml` is **excluded** from production deployment. It only exists locally for development with dummy SMTP values.

#### Port Mapping (Production)
- Frontend (nginx container): Port 8081 → nginx proxy → HTTPS 443
- Gateway: Port 8080 (internal only)
- User Management: Port 5001 (internal only)
- Inventory: Port 5002 (internal only)
- Sales: Port 5003 (internal only)
- Financial: Port 5004 (internal only)
- Dashboard: Port 5005 (internal only)
- Kafka UI: Port 9001 (changed from 9000 - Portainer conflict)

#### Nginx Reverse Proxy
- Configuration: `/etc/nginx/sites-available/shopping-now.net`
- SSL Certificates: `/etc/letsencrypt/live/shopping-now.net/`
- Routes:
  - `https://shopping-now.net/` → `http://localhost:8081` (frontend)
  - `https://shopping-now.net/api/` → `http://localhost:8080/` (gateway)

### Container Name Mapping (CRITICAL for Gateway Config)

**In Docker**: Services communicate using **container names** on the `erp-network`:
- `user-management:8080` (NOT `localhost:5001`)
- `inventory:8080` (NOT `localhost:5002`)
- `sales:8080` (NOT `localhost:5003`)
- `financial:8080` (NOT `localhost:5004`)
- `dashboard:8080` (NOT `localhost:5005`)

**Gateway Configuration**:
- Development: `appsettings.Development.json` uses `localhost:500X`
- Production: `appsettings.Production.json` uses `service-name:8080`
- Environment: Set `ASPNETCORE_ENVIRONMENT=Production` in docker-compose.yml

### Frontend Build Configuration

**CRITICAL**: `VITE_API_GATEWAY_URL` must be set at **build time** as a Docker build arg:

```yaml
# docker-compose.yml
frontend:
  build:
    context: ./frontend
    dockerfile: Dockerfile
    args:
      VITE_API_GATEWAY_URL: https://shopping-now.net  # No /api suffix!
```

**Why**: Vite bakes environment variables into the JavaScript bundle during build. Runtime env vars don't work.

### Troubleshooting Commands Reference

```bash
# Check all container health
docker-compose ps

# View real-time logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f gateway
docker-compose logs -f user-management
docker-compose logs -f frontend

# Check environment variables in container
docker exec erp-gateway printenv
docker exec erp-user-management printenv | grep Jwt

# Restart specific service
docker-compose restart gateway

# Rebuild and restart service
docker-compose up -d --build gateway

# Remove and recreate all containers
docker-compose down
docker-compose up -d --build

# Check nginx configuration
sudo nginx -t
sudo systemctl reload nginx

# View nginx logs
sudo tail -f /var/log/nginx/shopping-now.net.access.log
sudo tail -f /var/log/nginx/shopping-now.net.error.log

# Check SSL certificate
sudo certbot certificates
sudo certbot renew --dry-run

# Monitor Docker resource usage
docker stats

# Clean up unused Docker resources
docker system prune -a
```

### Quick Diagnosis Checklist

When production is down:

1. ✅ Check container status: `docker-compose ps`
2. ✅ Check gateway logs: `docker-compose logs --tail=100 gateway`
3. ✅ Verify JWT secrets match: `docker exec erp-gateway printenv | grep Jwt`
4. ✅ Check nginx is running: `sudo systemctl status nginx`
5. ✅ Test nginx config: `sudo nginx -t`
6. ✅ Check SSL certificate: `sudo certbot certificates`
7. ✅ Verify .env file exists: `cat .env`
8. ✅ Check DNS resolution: `nslookup shopping-now.net`

## 🤝 Getting Help

- **Issues**: Create a GitHub issue with details
- **Discussions**: Use GitHub Discussions for questions
- **Documentation**: Check service-specific READMEs
- **Production Issues**: SSH to server and check logs first

## 📊 Current Status

- **Backend Services**: ✅ Complete (6 services + gateway)
- **Frontend**: ✅ Complete (5 feature modules)
- **Testing**: 🔄 In Progress (14 passing tests, expanding coverage)
- **Documentation**: ✅ Complete
- **CI/CD**: 🔄 Ready for setup

**Overall Completion**: 95% - Production ready!

---

Thank you for contributing to the ERP System! 🎉
