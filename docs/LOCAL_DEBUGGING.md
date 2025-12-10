# Local Development & Debugging Guide

## 🎯 Recommended Approach: Local Services + Containerized Infrastructure

This is the **best approach** for local development with full debugging support in VS Code.

### How It Works

```
┌─────────────────────────────────────────────────────────┐
│  Your Machine                                           │
│                                                         │
│  ┌───────────────────────┐  ┌──────────────────────┐  │
│  │  .NET Services        │  │  Docker Containers   │  │
│  │  (Running Locally)    │  │  (Infrastructure)    │  │
│  │                       │  │                      │  │
│  │  • API Gateway:5000   │  │  • MongoDB:27017    │  │
│  │  • User Mgmt:5001     │──▶ • Kafka:9092       │  │
│  │  • Inventory:5002     │  │  • Prometheus:9090  │  │
│  │  • Sales:5003         │  │  • Grafana:3001     │  │
│  │  • Financial:5004     │  │                      │  │
│  │  • Dashboard:5005     │  │                      │  │
│  │                       │  │                      │  │
│  │  ✅ Full debugging    │  │  ✅ Isolated         │  │
│  │  ✅ Hot reload        │  │  ✅ Easy to manage   │  │
│  │  ✅ Fast iteration    │  │                      │  │
│  └───────────────────────┘  └──────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Step-by-Step Setup

#### 1. Start Infrastructure Only

```powershell
# Start MongoDB, Kafka, Prometheus, Grafana
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d

# Verify containers are running
docker ps
```

**Running:**
- MongoDB: localhost:27017
- Kafka: localhost:9092
- Kafka UI: http://localhost:9000
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3001

#### 2. Run Services Locally

**Option A: Use VS Code Tasks** (Easiest)

Press `Ctrl+Shift+P` → Type "Tasks: Run Task" → Select:
- `watch-all-services` - Starts all backend services
- `dev-frontend` - Starts React frontend

**Option B: Manual Terminal** (More Control)

Open terminals for each service:

```powershell
# Terminal 1 - API Gateway
cd services/gateway/ApiGateway
dotnet watch run

# Terminal 2 - User Management
cd services/user-management/UserManagement
dotnet watch run

# Terminal 3 - Inventory
cd services/inventory/InventoryManagement
dotnet watch run

# Terminal 4 - Sales
cd services/sales/SalesManagement
dotnet watch run

# Terminal 5 - Financial
cd services/financial/FinancialManagement
dotnet watch run

# Terminal 6 - Dashboard
cd services/dashboard/DashboardAnalytics
dotnet watch run

# Terminal 7 - Frontend
cd frontend
npm run dev
```

#### 3. Debug in VS Code

**Set Breakpoints:**
1. Open any `.cs` file
2. Click left of line number to set breakpoint (red dot)

**Start Debugging:**
1. Press `F5` or click "Run and Debug" in sidebar
2. Select the service you want to debug
3. Debugger attaches to running process
4. Hit your breakpoint!

**Or Create launch.json:**

Press `Ctrl+Shift+P` → "Debug: Open launch.json" → Add:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug User Management",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/services/user-management/UserManagement/bin/Debug/net8.0/UserManagement.dll",
      "args": [],
      "cwd": "${workspaceFolder}/services/user-management/UserManagement",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

---

## 🐳 Alternative: Remote Debugging Docker Containers

If you prefer to run everything in containers and still debug:

### Requirements

1. Install .NET SDK in containers (already done in Dockerfiles)
2. Expose debugger port
3. Attach VS Code to remote process

### Update docker-compose.yml for Debugging

Add to each service:

```yaml
user-management:
  build:
    context: ./services/user-management
    dockerfile: Dockerfile
    target: build  # Stop at build stage (includes SDK)
  container_name: erp-user-management
  ports:
    - "5001:8080"
    - "5101:5101"  # Debugger port
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - VSTEST_HOST_DEBUG=1  # Enable debugging
  volumes:
    - ./services/user-management:/src:cached  # Mount source
  command: dotnet watch run --no-restore
```

### Attach Debugger

1. Container must be running with debugger port exposed
2. In VS Code: `Ctrl+Shift+P` → "Attach to Process"
3. Select `.NET Core Docker Attach`
4. Choose container
5. Debug!

**But this is slower** than running locally. Use only if you need to test Docker-specific behavior.

---

## 📊 Configuration Files Explained

### appsettings.Development.json

These files override `appsettings.json` when `ASPNETCORE_ENVIRONMENT=Development`.

**User Management (`services/user-management/UserManagement/appsettings.Development.json`):**
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:admin123@localhost:27017",
    "DatabaseName": "erp_users"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

**Why `localhost`?**
- When running `dotnet watch run`, your service runs on your machine
- Docker containers expose ports to `localhost`
- So `localhost:27017` connects to containerized MongoDB

**Why not `mongodb-service`?**
- `mongodb-service` only works inside Docker network
- Your local machine doesn't know about Docker internal DNS

### appsettings.json (Production/Container)

Used when running in Docker containers:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:admin123@mongodb-service:27017"
  },
  "Kafka": {
    "BootstrapServers": "kafka-service:9092"
  }
}
```

**Why `mongodb-service`?**
- Inside Docker network, services find each other by name
- Docker has internal DNS that resolves `mongodb-service` → container IP

---

## 🔧 Common Scenarios

### Scenario 1: Debug One Service, Rest in Docker

```powershell
# Start all services in Docker
docker-compose up -d

# Stop the service you want to debug
docker-compose stop user-management

# Run it locally with debugger
cd services/user-management/UserManagement
dotnet watch run

# Now debug in VS Code with F5
```

### Scenario 2: Test Integration Between Services

```powershell
# Infrastructure only
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d

# Run all services locally
# Use VS Code task: "watch-all-services"

# Now they can call each other via localhost:5001, etc.
```

### Scenario 3: Hot Reload Frontend + Backend

```powershell
# Infrastructure
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d

# Backend with hot reload
cd services/user-management/UserManagement
dotnet watch run

# Frontend with hot reload
cd frontend
npm run dev

# Changes to .cs or .tsx files reload automatically!
```

---

## 🎓 Best Practices

### 1. Use `dotnet watch run` Always

- **Hot Reload**: Code changes reload automatically
- **Faster**: No container rebuild needed
- **Debugging**: Full VS Code debugger support

### 2. Keep Infrastructure in Docker

- **Isolation**: Won't conflict with local MongoDB/Kafka
- **Easy Cleanup**: `docker-compose down -v` removes everything
- **Consistency**: Same versions as production

### 3. Use Environment Variables

For sensitive data, use environment variables instead of appsettings:

```powershell
# Windows PowerShell
$env:MongoDB__ConnectionString = "mongodb://localhost:27017"
dotnet watch run

# Or create .env file (not committed to git)
```

### 4. Multiple Terminal Windows

Use VS Code's split terminal feature:
- `Ctrl+Shift+5` to create new terminal
- Run each service in separate terminal
- Easy to see logs from all services

---

## 🐛 Troubleshooting

### "Connection refused to localhost:27017"

**Solution:**
```powershell
# Check if MongoDB container is running
docker ps | Select-String mongodb

# If not running, start it
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d mongodb

# Check logs
docker logs erp-mongodb
```

### "Unable to connect to Kafka"

**Solution:**
```powershell
# Restart Kafka (it's finicky)
docker restart erp-kafka

# Wait 30 seconds for it to start
Start-Sleep -Seconds 30

# Test connection
docker exec erp-kafka kafka-broker-api-versions --bootstrap-server localhost:9092
```

### "Port already in use"

**Solution:**
```powershell
# Find process using port 5001
netstat -ano | findstr :5001

# Kill process (replace PID)
taskkill /PID <PID> /F

# Or change port in launchSettings.json
```

### "appsettings.Development.json not loading"

**Solution:**
```powershell
# Ensure ASPNETCORE_ENVIRONMENT is set
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet watch run

# Or verify in launchSettings.json:
# "environmentVariables": {
#   "ASPNETCORE_ENVIRONMENT": "Development"
# }
```

### "Services can't find each other"

**Problem:** API Gateway can't reach User Management

**Solution:**
- All services must run on their expected ports
- User Management: 5001
- Inventory: 5002
- Sales: 5003
- Financial: 5004
- Dashboard: 5005
- API Gateway: 5000 (routes to others)

Check with:
```powershell
curl http://localhost:5001/health/live
curl http://localhost:5002/health/live
```

---

## 📚 Summary

**For Daily Development:**
✅ Use `infrastructure/docker-compose.dev.yml` for MongoDB, Kafka  
✅ Run services with `dotnet watch run` for debugging  
✅ Full VS Code debugger support with breakpoints  
✅ Hot reload for fast iteration

**For Testing Full System:**
✅ Use root `docker-compose.yml`  
✅ Everything runs in containers  
✅ Tests Docker networking and configs

**When to Use What:**

| Scenario | Approach |
|----------|----------|
| Daily coding/debugging | Local services + Docker infrastructure |
| Testing integrations | All services local or all in Docker |
| Pre-deployment testing | Full Docker Compose |
| CI/CD pipeline | Kubernetes/Skaffold |

---

## 🚀 Quick Commands

```powershell
# Start infrastructure only
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d

# Run all services (use VS Code task or manually)
dotnet watch run  # In each service directory

# Stop infrastructure
docker-compose -f docker-compose.dev.yml down

# Full system in Docker
cd ..
docker-compose up -d

# Stop full system
docker-compose down
```

Happy debugging! 🐛✨
