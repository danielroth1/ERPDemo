# Demo ERP System

A comprehensive microservices-based ERP system demonstrating modern cloud-native architecture with User Management, Inventory, Sales & Orders, Financial Management, and Dashboard & Analytics modules.

## Quick Start

- install .net 8
- start Rancher Desktop / Docker Desktop
- start root folder in VS Code
- `dotnet tool restore` -> installs kiota

### Local Development

- install some docker environment, e.g. Kiota, Podman, or Docker Desktop
- run some tasks with: ctrl + p -> `Task: Run Task`
  - `infra: dev-infrastructure`
  - `backend: watch-all-services`
  - `backend: generate-all-api-clients`
  - `frontend: dev`

- close any local VS Code instance connecting to remote via ssh -> might block 8080 port

- `cd frontend` -> `npm install`
- Run & Debug -> `Launch Frontend (Chrome)` (or whatever browser you prefer)
  - alternatively: Run Task `dev-frontend`

#### Deploy to local Kubernetes

- run task with: ctrl + p -> `Task: Run Task`
  - `k8s: deploy-local`
- make sure to forward ports:
  - `k8s: port-forwards-frontend`
  - `k8s: port-forwards-gateway`
- open frontend in browser: `http://localhost:3002`
- optional: Run & Debug -> `K8s: Attach` -> select a service that you want the debugger to attach to
- optional: install `Kubernetes` extension in VS Code to work with running pods

## Frontend

## Generate Kiota API

- `npm run generate:api`
- `npm run generate:api:service`

### Generation of the Kiota Client

- Each service exposes it's OpenAPI via a `/swagger` address (e.g. `http://localhost:5003/swagger/index.html`)
  - and the corresponding OpenAPI json `http://localhost:5003/swagger/v1/swagger.json`
- Kiota detects this and generates a React frontend client from this
- this means the services must be running to generate the client
- updates are only possible once they are exposed by the service
- see `kiota-config.json` for the service -> address mapping

## 🏗️ Architecture

```
┌─────────────┐
│   Frontend  │ (React + Redux + TypeScript)
│  (Vite SPA) │
└──────┬──────┘
       │
       ▼
┌─────────────────┐
│   API Gateway   │ (YARP Proxy + JWT Auth + GraphQL)
└────────┬────────┘
         │
    ┌────┴────┬────────┬─────────┬──────────┐
    ▼         ▼        ▼         ▼          ▼
┌────────┐ ┌──────┐ ┌─────┐ ┌────────┐ ┌─────────┐
│  User  │ │Inven-│ │Sales│ │Financial│ │Dashboard│
│  Mgmt  │ │tory  │ │     │ │        │ │         │
└───┬────┘ └──┬───┘ └──┬──┘ └───┬────┘ └────┬────┘
    │         │        │        │           │
    └─────────┴────────┴────────┴───────────┘
                      │
                  ┌───▼───┐
                  │ Kafka │ (Event Bus)
                  └───────┘
                      │
                  ┌───▼────┐
                  │MongoDB │ (5 Databases)
                  └────────┘
```

**Monitoring**: Prometheus + Grafana  
**Orchestration**: Docker Compose (local), Kubernetes (production)

## 🚀 Technology Stack

| Component         | Technology                | Version      |
| ----------------- | ------------------------- | ------------ |
| Backend           | ASP.NET Core              | 9.0          |
| Frontend          | React + Vite + TypeScript | 19 / 7 / 5.9 |
| State Management  | Redux Toolkit             | 2.11         |
| Styling           | TailwindCSS               | 4.1          |
| Database          | MongoDB                   | 7.0          |
| Message Broker    | Apache Kafka (KRaft)      | 7.5          |
| API Gateway       | YARP                      | Latest       |
| GraphQL           | Hot Chocolate             | 13           |
| Real-time         | SignalR                   | 10           |
| Testing           | xUnit + Moq               | Latest       |
| Monitoring        | Prometheus + Grafana      | 2.48 / 10.2  |
| Logging           | Loki + Promtail           | 2.9          |
| Container Runtime | Docker / containerd       | Latest       |
| Orchestration     | Kubernetes                | 1.28+        |
| CI/CD             | GitHub Actions            | -            |
| Dev Tool          | Skaffold                  | Latest       |

## ✨ Features

### User Management

- User registration and authentication (JWT)
- Email/password management
- Role-based access control (Admin, Manager, User)
- Password reset via email

### Inventory Management

- Product catalog with CRUD operations
- Stock level tracking and adjustments
- Warehouse/location management
- Low-stock alerts

### Sales & Orders

- Order creation and management
- Customer information
- Invoice generation
- Order status workflow
- Stock validation

### Financial Management

- Double-entry ledger
- Expense/revenue tracking
- Payment status tracking
- Basic financial reports

### Dashboard & Analytics

- Real-time metrics and KPIs
- Interactive charts
- Event aggregation
- SignalR live updates

## 📋 Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) for Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (for local service development)
- [Node.js 20+](https://nodejs.org/) (for frontend development)
- Git
- PowerShell 7+ (recommended)

## 📋 Current Status

✅ **Infrastructure**: Complete (100%)  
✅ **User Management Service**: Complete (100%)  
✅ **Inventory Management Service**: Complete (100%)  
✅ **Sales & Orders Service**: Complete (100%)  
✅ **Financial Management Service**: Complete (100%)  
✅ **Dashboard & Analytics Service**: Complete (100%)  
✅ **API Gateway Service**: Complete (100%)  
✅ **React Frontend**: Complete (100%) - 5 feature modules, 576KB bundle  
✅ **Testing Infrastructure**: Complete (100%) - 14 passing tests  
🚧 **Expanded Test Coverage**: In Progress (30%)

**Overall Progress: 95%** 🎉

### Completed Components

- Project structure and infrastructure configurations
- User Management service with full authentication (JWT + refresh tokens)
- Inventory Management service with products, categories, stock tracking
- Sales & Orders service with orders, customers, invoices, GraphQL API
- Financial Management service with double-entry bookkeeping, budgets, reports
- Dashboard & Analytics service with Kafka consumers, GraphQL, SignalR real-time updates
- API Gateway service with YARP reverse proxy, JWT auth, rate limiting, 20 routes
- React frontend with authentication, dashboard, Redux, SignalR real-time updates
- MongoDB, Kafka, Prometheus, Grafana, Loki configurations
- Skaffold development automation (3 profiles)
- Docker Compose for local stack
- Kubernetes base manifests
- Linkerd service mesh setup
- cert-manager installation
- Comprehensive documentation (Local Dev, Deployment, Implementation guides)
- CI/CD pipeline with testing, coverage, and deployment automation

See [PROGRESS.md](docs/PROGRESS.md) for detailed status.

## 🏃 Quick Start (Windows Development)

### 1. Clone the repository

```powershell
git clone <repository-url>
cd erp
```

### 1.5 Stop existing Containers

```
docker-compose -f docker-compose.dev.yml down
```

### 2. Start Infrastructure (Docker Compose)

```powershell
# Start MongoDB, Kafka, Prometheus, Grafana
cd infrastructure
docker-compose up -d

# Wait for services to be ready (30-60 seconds)
docker-compose ps
```

### 3. Run Backend Services

```powershell
# Terminal 1 - API Gateway
cd services/gateway/ApiGateway
dotnet run

# Terminal 2 - User Management
cd services/user-management/UserManagement
dotnet run

# Terminal 3 - Inventory Management
cd services/inventory/InventoryManagement
dotnet run

cd services/sales/SalesManagement
dotnet run

# Continue for other services...
```

### 4. Run Frontend

```powershell
cd frontend
npm install
npm run dev
```

### 5. Access the Application

- **Frontend**: http://localhost:5173
- **API Gateway**: http://localhost:5001
- **Swagger UI**: http://localhost:5001/swagger
- **Grafana**: http://localhost:3000 (admin/admin)
- **Kafka UI**: http://localhost:8080

### Running with Kubernetes and Rancher Desktop

install skaffold

```
scoop bucket add extras
scoop install skaffold
```

### Production Deployment (Linux/Kubernetes)

For production deployment on Linux with Kubernetes and Linkerd service mesh, see [DEPLOYMENT.md](DEPLOYMENT.md)

## 📚 Documentation

- [Local Development Guide](docs/LOCAL_DEVELOPMENT.md)
- [Production Deployment](docs/DEPLOYMENT.md)
- [Architecture Details](docs/ARCHITECTURE.md)
- [API Documentation](docs/API_DOCUMENTATION.md)
- [Monitoring Guide](docs/MONITORING.md)
- [Testing Guide](docs/TESTING.md)
- [Repository Structure](.github/README.md)

## 🧪 Testing

```bash
# Backend tests
dotnet test

# Frontend tests
cd frontend
npm test

# E2E tests
cd tests/e2e
npm test
```

## 🚀 Production Deployment

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for detailed instructions on deploying to a self-hosted Linux server with K3s, cert-manager, and Let's Encrypt SSL.

## 📊 CI/CD Pipeline

GitHub Actions workflow automatically:

- Runs unit and integration tests
- Collects code coverage (70% threshold)
- Builds and pushes Docker images
- Scans for vulnerabilities
- Deploys to production via SSH
- Performs health checks and rollback on failure

## 🤝 Contributing

See [.github/README.md](.github/README.md) for repository structure, development workflow, and contribution guidelines.

## 📄 License

MIT

## 📧 Contact

For questions or support, please open an issue in the repository.
