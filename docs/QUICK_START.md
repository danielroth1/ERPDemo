# ERP System - Quick Start Guide

## 🎯 Local Development with MongoDB Container (Recommended)

This is the **fastest way** to develop on Windows with Rancher Desktop.

### Step 1: Start Infrastructure Only

```powershell
# Navigate to infrastructure folder
cd infrastructure

# Start MongoDB, Kafka, Prometheus, and Grafana
docker-compose up -d

# Verify containers are running
docker-compose ps
```

**What this does:**
- Starts MongoDB on `localhost:27017`
- Starts Kafka on `localhost:9092`
- Starts Prometheus on `localhost:9090`
- Starts Grafana on `localhost:3001`

### Step 2: Run Your Services Locally

Open **6 terminals** (or use VS Code tasks):

**Terminal 1 - API Gateway:**
```powershell
cd services/gateway/ApiGateway
dotnet watch run
```

**Terminal 2 - User Management:**
```powershell
cd services/user-management/UserManagement
dotnet watch run
```

**Terminal 3 - Inventory:**
```powershell
cd services/inventory/InventoryManagement
dotnet watch run
```

**Terminal 4 - Sales:**
```powershell
cd services/sales/SalesManagement
dotnet watch run
```

**Terminal 5 - Financial:**
```powershell
cd services/financial/FinancialManagement
dotnet watch run
```

**Terminal 6 - Dashboard:**
```powershell
cd services/dashboard/DashboardAnalytics
dotnet watch run
```

**Terminal 7 - Frontend:**
```powershell
cd frontend
npm run dev
```

### Step 3: Access Your Application

- **Frontend:** http://localhost:5173
- **API Gateway:** http://localhost:5000
- **User Management:** http://localhost:5001/swagger
- **Inventory:** http://localhost:5002/swagger
- **Sales:** http://localhost:5003/swagger
- **Financial:** http://localhost:5004/swagger
- **Dashboard:** http://localhost:5005/swagger
- **Grafana:** http://localhost:3001 (admin/admin)
- **Prometheus:** http://localhost:9090

### OR: Use VS Code Tasks

Press `Ctrl+Shift+P` → Type "Tasks: Run Task" → Select:

1. `infrastructure-up` - Start MongoDB, Kafka, etc.
2. `watch-all-services` - Start all .NET services
3. `dev-frontend` - Start React frontend

---

## 🐳 Full Docker Compose (Everything in Containers)


### Setup

For Production/Staging (Target Machine):

Copy .env.example to .env
Edit .env with real SMTP credentials
Run docker-compose up -d
The .env file stays on the target machine and is never committed to git (it's in .gitignore).

```powershell
cd infrastructure
docker-compose up -d
```

This starts **everything** in containers:
- All infrastructure (MongoDB, Kafka)
- All 6 backend services
- Frontend

**Note:** Slower iteration because code changes require container rebuilds.

---

## ☸️ Kubernetes with Skaffold (Production-like)

### Prerequisites

1. Rancher Desktop running with Kubernetes enabled
2. Skaffold installed: `choco install skaffold`

### Deploy Everything

```powershell
# From project root
skaffold dev --profile=local
```

**What Skaffold does:**
1. Builds Docker images for all 7 services (6 backend + frontend)
2. Deploys MongoDB, Kafka, Prometheus, Grafana
3. Deploys your services to Kubernetes
4. Sets up port forwarding
5. Watches for code changes and auto-rebuilds

**Access services:**
- Frontend: http://localhost:3000
- API Gateway: http://localhost:8080
- Grafana: http://localhost:3001

### Stop Skaffold

Press `Ctrl+C` in the terminal or run:
```powershell
skaffold delete
```

---

## 🗄️ Working with MongoDB Container

### Connect to MongoDB Shell

```powershell
# Using Docker
docker exec -it erp-mongodb mongosh -u admin -p admin123

# Using VS Code task
Ctrl+Shift+P → Tasks: Run Task → mongodb-shell
```

### Common MongoDB Commands

```javascript
// List all databases
show dbs

// Switch to user management database
use erp_users

// Show collections
show collections

// Query users
db.users.find().pretty()

// Count users
db.users.countDocuments()

// Find specific user
db.users.findOne({ email: "admin@erp.com" })

// Exit
exit
```

### View MongoDB Logs

```powershell
docker logs erp-mongodb -f
```

### Stop MongoDB

```powershell
cd infrastructure
docker-compose stop mongodb
```

### Remove MongoDB Data (Fresh Start)

```powershell
docker-compose down -v
docker-compose up -d
```

---

## 📊 Monitoring

### Grafana Dashboards

1. Open http://localhost:3001
2. Login: admin/admin
3. Navigate to Dashboards
4. Pre-configured dashboards for all services

### Prometheus Metrics

1. Open http://localhost:9090
2. Query examples:
   - `http_requests_total` - Total requests
   - `process_cpu_seconds_total` - CPU usage
   - `dotnet_total_memory_bytes` - Memory usage

---

## 🔧 Troubleshooting

### Port Already in Use

```powershell
# Find process using port 27017 (MongoDB)
netstat -ano | findstr :27017

# Kill the process
taskkill /PID <process_id> /F
```

### MongoDB Connection Failed

```powershell
# Check if MongoDB is running
docker ps | findstr mongodb

# Restart MongoDB
docker restart erp-mongodb

# Check logs for errors
docker logs erp-mongodb --tail 50
```

### Kafka Not Working

```powershell
# Check Kafka status
docker ps | findstr kafka

# View Kafka topics
docker exec erp-kafka kafka-topics --bootstrap-server localhost:9092 --list

# Restart Kafka
docker restart erp-kafka erp-zookeeper
```

### Service Won't Start

```powershell
# Check for compile errors
dotnet build

# Clean and rebuild
dotnet clean
dotnet build

# Check appsettings.json for correct MongoDB connection string
```

### Kubernetes Pods Not Running

```powershell
# Check pod status
kubectl get pods -n erp-local

# View pod logs
kubectl logs -n erp-local <pod-name>

# Describe pod for details
kubectl describe pod -n erp-local <pod-name>

# Delete and recreate
kubectl delete namespace erp-local
skaffold dev --profile=local
```

---

## 🎓 Key Concepts

### What's the Difference?

| Approach | Pros | Cons | Use When |
|----------|------|------|----------|
| **Local .NET + Docker Infrastructure** | ✅ Fast iteration<br>✅ Easy debugging<br>✅ Low resource usage | ❌ Not production-like | Daily development |
| **Full Docker Compose** | ✅ All containerized<br>✅ Consistent environment | ❌ Slower rebuilds<br>❌ More RAM needed | Testing integrations |
| **Kubernetes (Skaffold)** | ✅ Production-like<br>✅ Auto-scaling<br>✅ Service mesh | ❌ Complex setup<br>❌ High resource usage | Pre-production testing |

### When to Use Each

**Use Local .NET + Docker Infrastructure when:**
- You're actively coding and debugging
- You want instant feedback on code changes
- You're working on a single service

**Use Docker Compose when:**
- You need to test service-to-service communication
- You want to test the entire system together
- You're preparing for deployment

**Use Kubernetes when:**
- You need to test Kubernetes-specific features
- You're validating deployment configurations
- You want to test in a production-like environment

---

## 📚 Additional Resources

- [Complete Docker & Kubernetes Guide](./KUBERNETES_DOCKER_GUIDE.md)
- [Deployment Guide](./DEPLOYMENT.md)
- [Local Development Guide](./LOCAL_DEVELOPMENT.md)

---

## 🚀 Recommended Workflow

**For most development:**
```powershell
# 1. Start infrastructure
cd infrastructure
docker-compose up -d

# 2. Run services with VS Code tasks
Ctrl+Shift+P → Tasks: Run Task → watch-all-services

# 3. Start frontend
Ctrl+Shift+P → Tasks: Run Task → dev-frontend
```

**Before committing code:**
```powershell
# Test with full Docker Compose
cd infrastructure
docker-compose up -d

# Or test with Kubernetes
skaffold dev --profile=local
```

---

## 💡 Pro Tips

1. **Use VS Code Tasks:** Much easier than managing terminals
2. **Watch Mode:** Always use `dotnet watch run` for hot reload
3. **MongoDB Compass:** Install MongoDB Compass GUI for easier database exploration
4. **Docker Desktop Alternative:** Rancher Desktop is lighter and Kubernetes-ready
5. **Resource Limits:** If your machine struggles, use local .NET + Docker infrastructure approach

---

**Need Help?** Check the [complete guide](./KUBERNETES_DOCKER_GUIDE.md) for detailed explanations!
