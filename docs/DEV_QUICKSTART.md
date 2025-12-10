# 🚀 Quick Start - Local Development

## TL;DR - Fastest Way to Start Developing

### One-Command Start (VS Code Tasks)

Press `Ctrl+Shift+P` → Type "Tasks: Run Task" → Select **`dev-setup`**

This starts:
- ✅ MongoDB, Kafka, Prometheus, Grafana (Docker)
- ✅ All 6 backend services (Local with hot reload)
- ✅ Frontend (Local with hot reload)

**Done!** You can now:
- Set breakpoints in VS Code
- Edit code with hot reload
- Debug with F5

---

## Manual Start (More Control)

### Step 1: Infrastructure

```powershell
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d
```

**Or use VS Code task:** `dev-infrastructure`

### Step 2: Backend Services

**Option A: All at once (VS Code task)**
- Task: `watch-all-services`

**Option B: One by one**
```powershell
# In separate terminals
cd services/gateway/ApiGateway && dotnet watch run
cd services/user-management/UserManagement && dotnet watch run
cd services/inventory/InventoryManagement && dotnet watch run
cd services/sales/SalesManagement && dotnet watch run
cd services/financial/FinancialManagement && dotnet watch run
cd services/dashboard/DashboardAnalytics && dotnet watch run
```

**Or use individual VS Code tasks:**
- `watch-gateway`
- `watch-user-management`
- `watch-inventory`
- `watch-sales`
- `watch-financial`
- `watch-dashboard`

### Step 3: Frontend

```powershell
cd frontend
npm run dev
```

**Or use VS Code task:** `dev-frontend`

---

## Access Your Services

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:5173 | React UI |
| **API Gateway** | http://localhost:5000 | Main entry point |
| **User Management** | http://localhost:5001/swagger | User API + Swagger |
| **Inventory** | http://localhost:5002/swagger | Inventory API + Swagger |
| **Sales** | http://localhost:5003/swagger | Sales API + Swagger |
| **Financial** | http://localhost:5004/swagger | Financial API + Swagger |
| **Dashboard** | http://localhost:5005/swagger | Dashboard API + Swagger |
| **Grafana** | http://localhost:3001 | Monitoring (admin/admin) |
| **Prometheus** | http://localhost:9090 | Metrics |
| **Kafka UI** | http://localhost:9000 | Kafka monitoring |
| **MongoDB** | localhost:27017 | Database (admin/admin123) |

---

## Debugging

### Set Breakpoint
1. Open any `.cs` file
2. Click left of line number (red dot appears)

### Start Debugging
- Press **F5**
- Or click "Run and Debug" in sidebar
- Hit endpoint to trigger breakpoint

### View Variables
- Hover over variables
- Check "Variables" panel
- Use Debug Console for expressions

---

## Common Commands

### Stop Everything
```powershell
# Stop infrastructure
cd infrastructure
docker-compose -f docker-compose.dev.yml down

# Stop services: Ctrl+C in each terminal
```

**Or use VS Code task:** `stop-dev-infrastructure`

### Restart MongoDB
```powershell
docker restart erp-mongodb
```

### Restart Kafka
```powershell
docker restart erp-kafka
```

### View Logs
```powershell
# Infrastructure logs
docker logs erp-mongodb -f
docker logs erp-kafka -f

# Service logs: Check terminal where service is running
```

### Clean Database
```powershell
cd infrastructure
docker-compose -f docker-compose.dev.yml down -v  # Removes volumes
docker-compose -f docker-compose.dev.yml up -d
```

---

## Configuration

### appsettings.Development.json

Services automatically use `localhost` for MongoDB/Kafka when running locally:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:admin123@localhost:27017"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092"
  }
}
```

**Already configured!** No changes needed.

---

## VS Code Tasks Quick Reference

| Task | What It Does |
|------|-------------|
| `dev-setup` | 🎯 **START HERE** - Starts everything |
| `dev-infrastructure` | Start MongoDB, Kafka only |
| `watch-all-services` | Start all backend services |
| `watch-gateway` | Start API Gateway only |
| `watch-user-management` | Start User Management only |
| `watch-inventory` | Start Inventory only |
| `watch-sales` | Start Sales only |
| `watch-financial` | Start Financial only |
| `watch-dashboard` | Start Dashboard only |
| `dev-frontend` | Start React frontend |
| `stop-dev-infrastructure` | Stop infrastructure |
| `build-all-services` | Build all services |

**Access tasks:** `Ctrl+Shift+P` → "Tasks: Run Task"

---

## Troubleshooting

### "Port already in use"
```powershell
# Find process on port 5001
netstat -ano | findstr :5001

# Kill it
taskkill /PID <PID> /F
```

### "Can't connect to MongoDB"
```powershell
# Check if running
docker ps | Select-String mongodb

# Start if not running
cd infrastructure
docker-compose -f docker-compose.dev.yml up -d mongodb

# Check logs
docker logs erp-mongodb
```

### "Can't connect to Kafka"
```powershell
# Restart Kafka (it's temperamental)
docker restart erp-kafka

# Wait 30 seconds
Start-Sleep -Seconds 30
```

### "Hot reload not working"
- Make sure you're using `dotnet watch run` (not `dotnet run`)
- Check if file is saved
- Try: `Ctrl+C` → `dotnet watch run` again

---

## Need More Help?

📖 Full guide: [`docs/LOCAL_DEBUGGING.md`](./LOCAL_DEBUGGING.md)  
🐛 Troubleshooting: [`docs/LOCAL_DEBUGGING.md#troubleshooting`](./LOCAL_DEBUGGING.md#troubleshooting)

---

**Happy Coding! 🎉**
