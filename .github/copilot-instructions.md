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
| API Gateway | 5001 | http://localhost:5001 |
| User Management | 5002 | http://localhost:5002 |
| Inventory Management | 5003 | http://localhost:5003 |
| Sales & Orders | 5004 | http://localhost:5004 |
| Financial Management | 5005 | http://localhost:5005 |
| Dashboard & Analytics | 5006 | http://localhost:5006 |
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
# Run all tests
dotnet test

# Run specific service tests
cd services/user-management
dotnet test --verbosity minimal

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

### Test Structure

- **Model Tests**: Validate entity properties, defaults, validation
- **Service Tests**: Test business logic with mocked dependencies
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
  2. Run `npm run generate:api` to regenerate all Kiota clients
  3. **IMPORTANT**: Test that the frontend still builds:
     - If a watch task is running (e.g., `dev-frontend`), observe for compilation errors
     - Otherwise, run `npm run build` to verify the build succeeds
     - Fix any TypeScript errors that arise from API changes
  4. Always use the generated clients from `frontend/src/generated/clients/` instead of creating custom API calls
  5. If Kiota reports OpenAPI errors (e.g., invalid schema keys), fix the backend DTOs/responses causing the issue

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

3. **Push to remote**
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

### Backend Services

1. **Check service health**: `http://localhost:500X/health/ready`
2. **View Swagger docs**: `http://localhost:500X/swagger`
3. **Check logs**: Services log to console in JSON format
4. **MongoDB**: Connect with MongoDB Compass to `mongodb://localhost:27017`
5. **Kafka**: Access Kafka UI at `http://localhost:8080`

### Frontend

1. **Redux DevTools**: Install extension to inspect state
2. **Network tab**: Monitor API calls in browser DevTools
3. **React DevTools**: Install extension to inspect components
4. **Console logs**: Check for errors and warnings

### Common Issues

| Issue | Solution |
|-------|----------|
| Service won't start | Check MongoDB/Kafka are running (`docker-compose ps`) |
| 401 Unauthorized | Verify JWT token is valid and not expired |
| CORS errors | Ensure frontend origin is in Gateway CORS policy |
| Database connection failed | Check connection string in `appsettings.json` |
| Port already in use | Change port in `appsettings.json` or kill process |

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
- [ ] **Kiota API client generated** (run `scripts/generate-api-clients.ps1`)
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
