# Rancher Desktop Setup for k3s + nerdctl

This project is configured to use **Rancher Desktop in k3s mode** with **nerdctl** as the Docker-compatible CLI.

## Why nerdctl?

- ✅ Works seamlessly with Rancher Desktop's k3s + containerd
- ✅ Docker CLI compatible syntax
- ✅ Supports Docker Compose files (`nerdctl compose`)
- ✅ No need to switch between moby and k3s modes
- ✅ Use both containers AND Kubernetes simultaneously

## Setup Steps

### 1. Configure Rancher Desktop

1. Open **Rancher Desktop**
2. Go to **Preferences/Settings** → **Kubernetes**
3. Set **Kubernetes version**: Latest stable
4. Set **Container Runtime**: **containerd** (default for k3s)
5. **Enable Kubernetes**: ✅ Checked
6. Click **Apply & Restart**

### 2. Verify nerdctl is Available

```powershell
# Check nerdctl version
nerdctl --version

# Check nerdctl compose
nerdctl compose version

# If not in PATH, add Rancher Desktop to your PATH:
# Default location: C:\Program Files\Rancher Desktop\resources\resources\win32\bin
```

### 3. Test Installation

```powershell
# List containers
nerdctl ps

# Test compose
cd infrastructure
nerdctl compose -f docker-compose.dev.yml version
```

## Quick Command Reference

### Container Management

```powershell
# List containers
nerdctl ps

# List all containers (including stopped)
nerdctl ps -a

# View logs
nerdctl logs <container-name>

# Execute command in container
nerdctl exec -it <container-name> sh

# Stop container
nerdctl stop <container-name>

# Remove container
nerdctl rm <container-name>
```

### Compose Operations

```powershell
# Start services
nerdctl compose -f docker-compose.dev.yml up -d

# Stop services
nerdctl compose -f docker-compose.dev.yml down

# View logs
nerdctl compose -f docker-compose.dev.yml logs -f

# Restart service
nerdctl compose -f docker-compose.dev.yml restart <service-name>

# View status
nerdctl compose -f docker-compose.dev.yml ps
```

### Image Management

```powershell
# List images
nerdctl images

# Pull image
nerdctl pull postgres:16-alpine

# Remove image
nerdctl rmi <image-id>

# Build image
nerdctl build -t myimage:tag .
```

## VS Code Tasks

All VS Code tasks have been updated to use `nerdctl`:

- ✅ `dev-infrastructure` - Start infrastructure with nerdctl compose
- ✅ `stop-dev-infrastructure` - Stop infrastructure
- ✅ `infrastructure-up` - Alternative infrastructure start
- ✅ `infrastructure-down` - Stop all infrastructure
- ✅ `infrastructure-logs` - View infrastructure logs
- ✅ `mongodb-shell` - Connect to MongoDB
- ✅ `kafka-topics-list` - List Kafka topics

**No changes needed to your workflow!** Just run tasks as before.

## Development Workflow

### Option 1: Infrastructure + Local Services (Recommended)

```powershell
# 1. Start infrastructure (PostgreSQL, Kafka, etc.)
# Terminal → Run Task → dev-infrastructure

# 2. Run backend services locally with hot reload
# Terminal → Run Task → watch-all-services

# 3. Run frontend dev server
# Terminal → Run Task → dev-frontend
```

### Option 2: Kubernetes Testing

```powershell
# Deploy everything to k3s
kubectl apply -k infrastructure/k8s/local/

# Check status
kubectl get pods -n erp-local
kubectl get svc -n erp-local

# View logs
kubectl logs -f -n erp-local <pod-name>

# Delete deployment
kubectl delete namespace erp-local
```

### Option 3: Full Docker Compose (Production Simulation)

```powershell
# Start entire stack
nerdctl compose up -d

# View logs
nerdctl compose logs -f

# Stop stack
nerdctl compose down
```

## Kubernetes + Containers Simultaneously

The beauty of k3s + nerdctl is you can run both:

```powershell
# Terminal 1: Run infrastructure with nerdctl compose
nerdctl compose -f infrastructure/docker-compose.dev.yml up -d

# Terminal 2: Deploy app to Kubernetes
kubectl apply -k infrastructure/k8s/local/

# They coexist peacefully!
```

## Troubleshooting

### nerdctl: command not found

**Solution**: Add Rancher Desktop to PATH

```powershell
# Windows: Add to System PATH
C:\Program Files\Rancher Desktop\resources\resources\win32\bin

# Or use full path temporarily
& "C:\Program Files\Rancher Desktop\resources\resources\win32\bin\nerdctl.exe" ps
```

### Containers not visible in Rancher Desktop UI

This is normal. Rancher Desktop UI shows Kubernetes pods, not containerd containers. Use `nerdctl ps` instead.

### Permission denied errors

Run PowerShell or Command Prompt as Administrator (required for nerdctl on Windows).

### LoadBalancer services stuck in Pending

k3s includes ServiceLB (LoadBalancer support). If IPs are pending:

```powershell
# Check k3s LoadBalancer
kubectl get svc -n kube-system
kubectl logs -n kube-system -l app=svclb-<service-name>

# Alternative: Use NodePort
kubectl patch svc <service-name> -n erp-local -p '{"spec":{"type":"NodePort"}}'
```

### Can't connect to PostgreSQL/Kafka from local services

Make sure ports are forwarded correctly:

```powershell
# Check what ports are exposed
nerdctl compose -f infrastructure/docker-compose.dev.yml ps

# PostgreSQL should be on 5433 (not 5432 to avoid conflicts)
# Kafka should be on 9092
```

## Switching Back to Docker Desktop (Not Recommended)

If you need to use Docker Desktop instead:

1. Close Rancher Desktop
2. Start Docker Desktop
3. Update `.vscode/tasks.json` - change all `nerdctl` back to `docker`/`docker-compose`
4. Restart VS Code

**Note**: You'll lose the ability to use Kubernetes + containers simultaneously.

## Best of Both Worlds

With this setup, you get:

- ✅ **Fast development**: Hot reload with local services
- ✅ **Container support**: nerdctl for infrastructure
- ✅ **Kubernetes testing**: k3s for production-like deployments
- ✅ **LoadBalancer support**: k3s ServiceLB built-in
- ✅ **Single tool**: No switching between moby and k3s
- ✅ **VS Code integration**: All tasks work seamlessly

Enjoy! 🚀
