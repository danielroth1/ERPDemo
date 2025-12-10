# Local Development Guide

Complete guide for setting up and running the ERP system locally using Rancher Desktop.

## 📋 Prerequisites

### Required Software

1. **Rancher Desktop** (v1.10+)
   - Download: https://rancherdesktop.io/
   - Container Engine: containerd or dockerd
   - Kubernetes: Enabled (v1.28+)
   - Resources: 8GB RAM, 4 CPU cores

2. **kubectl** (Usually included with Rancher Desktop)
   ```powershell
   kubectl version --client
   ```

3. **Skaffold** (v2.8+)
   ```powershell
   # Windows (Scoop)
   scoop install skaffold
   
   # Or download from https://skaffold.dev/docs/install/
   ```

4. **Linkerd CLI** (v2.14+)
   ```powershell
   # Windows (Scoop)
   scoop install linkerd2
   
   # Or download from https://linkerd.io/2/getting-started/
   ```

5. **.NET 8 SDK** (for local service development)
   - Download: https://dotnet.microsoft.com/download/dotnet/8.0

6. **Node.js 20+** (for frontend development)
   - Download: https://nodejs.org/

### Optional Tools

- **k9s** - Kubernetes CLI UI
  ```powershell
  scoop install k9s
  ```

- **kubectx/kubens** - Context/namespace switching
  ```powershell
  scoop install kubectx
  ```

## 🚀 Initial Setup

### 1. Install Rancher Desktop

1. Download installer from https://rancherdesktop.io/
2. Run installer
3. Launch Rancher Desktop
4. Go to **Preferences**:
   - **Kubernetes Settings**:
     - Enable Kubernetes: ✓
     - Kubernetes Version: v1.28 or later
   - **Container Engine**: containerd (recommended) or dockerd
   - **Resources**:
     - Memory: 8 GB
     - CPUs: 4
   - **WSL** (Windows):
     - WSL Integration: Enabled

5. Click **Apply** and wait for Kubernetes to start

### 2. Verify Kubernetes Context

```powershell
# Check current context
kubectl config current-context

# Should show: rancher-desktop

# If not, switch to it
kubectl config use-context rancher-desktop

# Verify cluster is running
kubectl cluster-info
kubectl get nodes
```

### 3. Install Linkerd Service Mesh

```powershell
# Pre-flight check
linkerd check --pre

# Install Linkerd CRDs
linkerd install --crds | kubectl apply -f -

# Install Linkerd control plane
linkerd install | kubectl apply -f -

# Verify installation
linkerd check

# Install Linkerd Viz extension (dashboard and metrics)
linkerd viz install | kubectl apply -f -

# Verify Viz installation
linkerd viz check
```

### 4. Clone Repository and Setup

```powershell
# Clone repository
git clone <repository-url>
cd erp

# Copy environment template
Copy-Item .env.example .env

# Edit .env with your SMTP credentials
notepad .env
```

### 5. Create Kubernetes Namespace

```powershell
# Create namespace for local development
kubectl create namespace erp-local

# Enable Linkerd injection for the namespace
kubectl annotate namespace erp-local linkerd.io/inject=enabled

# Verify namespace
kubectl get namespace erp-local -o yaml
```

## 🏗️ Building and Running

### Option 1: Using Skaffold (Recommended)

Skaffold provides hot-reload and automatic rebuilding during development.

```powershell
# Start development mode (watches for file changes)
skaffold dev --profile=local

# This will:
# 1. Build all Docker images
# 2. Deploy to erp-local namespace
# 3. Set up port forwarding
# 4. Stream logs from all pods
# 5. Watch for file changes and rebuild automatically

# Stop with Ctrl+C
```

**Access Services:**
- Frontend: http://localhost:3000
- API Gateway: http://localhost:8080
- Grafana: http://localhost:3001

**View Logs:**
All logs are streamed in the terminal where Skaffold is running.

### Option 2: Using Docker Compose (Without Kubernetes)

For simpler setup without Kubernetes overhead:

```powershell
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Access services (same ports as above)

# Stop services
docker-compose down
```

### Option 3: Manual Deployment

```powershell
# Build Docker images manually
docker build -t erp/user-management:latest ./services/user-management
docker build -t erp/inventory:latest ./services/inventory
# ... repeat for other services

# Deploy to Kubernetes
kubectl apply -f infrastructure/k8s/base/
kubectl apply -f infrastructure/k8s/local/

# Wait for pods to be ready
kubectl get pods -n erp-local -w

# Set up port forwarding
kubectl port-forward -n erp-local svc/frontend 3000:80
kubectl port-forward -n erp-local svc/gateway 8080:80
kubectl port-forward -n erp-local svc/grafana 3001:3000
```

## 🔍 Accessing Services

### Linkerd Dashboard

```powershell
# Open Linkerd dashboard in browser
linkerd viz dashboard

# View specific service metrics
linkerd viz stat deployments -n erp-local

# Live traffic view
linkerd viz tap deployment/user-management -n erp-local

# Service topology
linkerd viz edges deployment -n erp-local
```

### Kafka UI

```powershell
# Port forward Kafka UI
kubectl port-forward -n erp-local svc/kafka-ui-service 9000:8080

# Open browser to http://localhost:9000
```

### Grafana Dashboards

```powershell
# Access Grafana (via Skaffold port-forward or manual)
# URL: http://localhost:3001
# Default credentials: admin / admin
```

Pre-configured dashboards:
- ERP Overview - System-wide metrics
- Service Latencies - Response time percentiles
- Database Performance - MongoDB metrics
- Kafka Metrics - Message throughput and lag
- Business Metrics - Orders, revenue, users

### MongoDB

```powershell
# Connect to MongoDB
kubectl exec -it -n erp-local mongodb-0 -- mongosh -u admin -p admin123

# Use specific database
use erp_users

# Query collections
db.users.find()
```

## 🛠️ Development Workflow

### Making Changes to Backend Services

1. **Edit code** in `services/<service-name>/`

2. **Skaffold hot-reload** (if running `skaffold dev`):
   - Detects changes automatically
   - Rebuilds affected containers
   - Redeploys to cluster
   - No manual intervention needed

3. **Manual rebuild** (if not using Skaffold):
   ```powershell
   # Rebuild specific service
   docker build -t erp/user-management:latest ./services/user-management
   
   # Restart deployment
   kubectl rollout restart deployment/user-management -n erp-local
   ```

### Making Changes to Frontend

1. **Edit code** in `frontend/src/`

2. **With Skaffold**:
   - Auto-rebuild and refresh

3. **Local development** (faster iteration):
   ```powershell
   cd frontend
   npm install
   npm run dev
   
   # Access at http://localhost:5173 (Vite dev server)
   ```

### Running Tests

```powershell
# Backend tests
dotnet test

# Frontend tests
cd frontend
npm test

# E2E tests
cd tests/e2e
npm test

# Run tests in watch mode
dotnet watch test  # Backend
npm test -- --watch  # Frontend
```

### Viewing Logs

```powershell
# All pods in namespace
kubectl logs -f -n erp-local --all-containers=true

# Specific service
kubectl logs -f -n erp-local deployment/user-management

# Previous instance (after crash)
kubectl logs -n erp-local deployment/user-management --previous

# Logs from last hour with Loki (via Grafana Explore)
{namespace="erp-local"} |= "error"
```

### Debugging

```powershell
# Get pod details
kubectl describe pod -n erp-local <pod-name>

# Execute commands in pod
kubectl exec -it -n erp-local <pod-name> -- /bin/sh

# Check events
kubectl get events -n erp-local --sort-by='.lastTimestamp'

# Check resource usage
kubectl top pods -n erp-local

# View Linkerd proxy logs
kubectl logs -n erp-local <pod-name> -c linkerd-proxy
```

## 🧪 Testing Locally

### Test User Account Creation

```bash
# Register new user
curl -X POST http://localhost:8080/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!",
    "firstName": "Test",
    "lastName": "User"
  }'

# Login
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Password123!"
  }'
```

### Test Kafka Events

```powershell
# Port forward Kafka
kubectl port-forward -n erp-local svc/kafka-service 9092:9092

# Install kcat (Kafka CLI)
scoop install kcat

# List topics
kcat -L -b localhost:9092

# Consume messages from topic
kcat -C -b localhost:9092 -t user-events

# Produce test message
echo '{"eventType":"UserCreated","userId":"123"}' | kcat -P -b localhost:9092 -t user-events
```

## 🔧 Troubleshooting

### Rancher Desktop Not Starting

1. Check Docker Desktop is not running (conflict)
2. Restart Rancher Desktop
3. Check WSL2 is installed: `wsl --status`
4. Update WSL: `wsl --update`

### Pods Stuck in Pending

```powershell
kubectl describe pod -n erp-local <pod-name>

# Common issues:
# - Insufficient resources: Increase RAM/CPU in Rancher Desktop
# - PVC not bound: Check storage provisioner
# - Image pull error: Check image name and registry
```

### Linkerd Injection Not Working

```powershell
# Verify namespace annotation
kubectl get namespace erp-local -o yaml | grep inject

# Re-annotate if needed
kubectl annotate namespace erp-local linkerd.io/inject=enabled --overwrite

# Restart pods
kubectl rollout restart deployment -n erp-local <deployment-name>

# Check injection status
kubectl get pod -n erp-local <pod-name> -o jsonpath='{.metadata.annotations.linkerd\.io/inject-status}'
```

### MongoDB Connection Issues

```powershell
# Check MongoDB pod is running
kubectl get pod -n erp-local -l app=mongodb

# Check MongoDB logs
kubectl logs -n erp-local mongodb-0

# Test connection from another pod
kubectl run -it --rm debug --image=mongo:7 --restart=Never -- mongosh mongodb://admin:admin123@mongodb-service:27017
```

### Skaffold Build Failures

```powershell
# Clean build cache
skaffold build --cache-artifacts=false

# Verbose output
skaffold dev -v debug

# Skip tests during build (temporarily)
skaffold dev --skip-tests
```

### Port Already in Use

```powershell
# Find process using port
Get-Process -Id (Get-NetTCPConnection -LocalPort 3000).OwningProcess

# Kill process
Stop-Process -Id <process-id>

# Or use different ports in skaffold.yaml
```

## 🧹 Cleanup

### Stop Development

```powershell
# Stop Skaffold (Ctrl+C)

# Or delete resources
skaffold delete

# Or delete namespace
kubectl delete namespace erp-local
```

### Reset Everything

```powershell
# Delete all resources
kubectl delete namespace erp-local

# Uninstall Linkerd
linkerd viz uninstall | kubectl delete -f -
linkerd uninstall | kubectl delete -f -

# Reset Rancher Desktop (Preferences > Reset Kubernetes)
```

## 📚 Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Linkerd Documentation](https://linkerd.io/2/overview/)
- [Skaffold Documentation](https://skaffold.dev/docs/)
- [Rancher Desktop Documentation](https://docs.rancherdesktop.io/)

## 🆘 Getting Help

If you encounter issues:
1. Check logs: `kubectl logs -n erp-local <pod-name>`
2. Check events: `kubectl get events -n erp-local`
3. Check Linkerd: `linkerd check`
4. Search issues in GitHub repository
5. Open new issue with:
   - Error message
   - Steps to reproduce
   - Environment details (OS, Rancher Desktop version)
   - Output of `kubectl get pods -n erp-local`
