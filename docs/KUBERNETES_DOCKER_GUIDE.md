# Kubernetes & Docker Complete Guide for ERP System

## Table of Contents
1. [Core Concepts](#core-concepts)
2. [How It All Works Together](#how-it-all-works-together)
3. [Local Development Setup](#local-development-setup)
4. [Using MongoDB Container](#using-mongodb-container)
5. [Kubernetes Manifests Explained](#kubernetes-manifests-explained)
6. [Available Tasks](#available-tasks)

---

## Core Concepts

### What is a Kubernetes Manifest?

A **Kubernetes manifest** is a YAML file that describes the desired state of a Kubernetes resource. It tells Kubernetes:
- What to run (which containers/images)
- How to run it (ports, environment variables, resources)
- How to access it (services, networking)
- How many replicas to maintain

**Example Manifest Structure:**
```yaml
apiVersion: apps/v1        # API version
kind: Deployment           # Type of resource
metadata:
  name: user-management    # Name of the resource
spec:
  replicas: 1             # How many pods to run
  template:
    spec:
      containers:
      - name: user-management
        image: erp/user-management  # Docker image to use
        ports:
        - containerPort: 8080
```

### How Docker & Kubernetes Relate

```
┌─────────────────────────────────────────────────────────────┐
│                    YOUR APPLICATION                         │
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │
│  │   Code      │  │  Dockerfile │  │   Image     │       │
│  │  (.cs files)│─▶│ (instructions)│─▶│  (packaged) │      │
│  └─────────────┘  └─────────────┘  └──────┬──────┘       │
│                                            │               │
│                                            ▼               │
│                            ┌───────────────────────┐      │
│                            │  Container (running)  │      │
│                            │  - Has its own OS     │      │
│                            │  - Isolated process   │      │
│                            │  - Runs your app      │      │
│                            └───────────────────────┘      │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    KUBERNETES                                │
│                                                             │
│  Orchestrates multiple containers:                          │
│  - Schedules where containers run                          │
│  - Manages networking between containers                   │
│  - Handles scaling and restarts                            │
│  - Provides service discovery                              │
│                                                             │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐          │
│  │   Pod 1    │  │   Pod 2    │  │   Pod 3    │          │
│  │ Container  │  │ Container  │  │ Container  │          │
│  └────────────┘  └────────────┘  └────────────┘          │
└─────────────────────────────────────────────────────────────┘
```

### Docker Compose vs Kubernetes

| Feature | Docker Compose | Kubernetes |
|---------|---------------|------------|
| **Use Case** | Local development, simple deployments | Production, complex systems |
| **Configuration** | `docker-compose.yml` | Multiple YAML manifests |
| **Orchestration** | Single machine | Multi-machine clusters |
| **Scaling** | Manual | Automatic |
| **Health Checks** | Basic | Advanced (liveness, readiness) |
| **Service Discovery** | Container names | DNS-based services |
| **Load Balancing** | Basic | Advanced with ingress |

**Key Insight:** Docker Compose is like running apps on your laptop. Kubernetes is like running apps on a data center.

---

## How It All Works Together

### The Complete Flow

```
1. DEVELOPMENT
   ├─ Write C# code (Program.cs, Controllers, etc.)
   └─ Test locally with `dotnet run`

2. DOCKERIZATION
   ├─ Create Dockerfile (instructions to build image)
   ├─ Build image: `docker build -t erp/user-management .`
   └─ Result: Docker image stored locally

3. DEPLOYMENT OPTIONS

   A. Docker Compose (Simple)
      ├─ docker-compose.yml defines all services
      ├─ Command: `docker-compose up -d`
      └─ All containers run on your machine

   B. Kubernetes (Production-like)
      ├─ Manifests define desired state
      ├─ Skaffold orchestrates build + deploy
      ├─ Command: `skaffold dev --profile=local`
      └─ Kubernetes creates pods, services, etc.
```

### Your Project Structure

```
infrastructure/
├── docker-compose.yml          ← Docker Compose definition
│   └── Defines: MongoDB, Kafka, all services
│
├── k8s/                        ← Kubernetes manifests
│   ├── base/                   ← Base configurations
│   │   ├── namespace.yaml      ← Creates erp-local namespace
│   │   ├── mongodb.yaml        ← MongoDB deployment
│   │   ├── kafka.yaml          ← Kafka deployment
│   │   ├── user-management.yaml ← Your service
│   │   ├── inventory.yaml
│   │   ├── sales.yaml
│   │   ├── financial.yaml
│   │   ├── dashboard.yaml
│   │   ├── gateway.yaml
│   │   ├── frontend.yaml
│   │   ├── prometheus.yaml     ← Monitoring
│   │   └── grafana.yaml
│   │
│   ├── local/                  ← Local dev overrides
│   │   └── kustomization.yaml  ← Combines base manifests
│   │
│   └── production/             ← Production configs
│       ├── kustomization.yaml
│       ├── ingress.yaml        ← HTTPS/TLS setup
│       └── secrets.yaml

services/
├── user-management/
│   ├── Dockerfile              ← Builds Docker image
│   └── UserManagement/
│       └── Program.cs          ← Your application

skaffold.yaml                   ← Orchestrates everything
```

---

## Local Development Setup

### Scenario 1: Docker Compose Only (Recommended for Windows)

**Best for:** Quick development, debugging in VS Code

```powershell
# 1. Start infrastructure (MongoDB, Kafka, monitoring)
cd infrastructure
docker-compose up -d

# 2. Verify services are running
docker-compose ps

# Expected output:
# NAME                STATUS    PORTS
# erp-mongodb         running   27017->27017
# erp-kafka           running   9092->9092
# erp-prometheus      running   9090->9090
# erp-grafana         running   3001->3001

# 3. Run your .NET services locally (NOT in containers)
cd ../services/user-management/UserManagement
dotnet run

# 4. In another terminal, run frontend
cd ../../../frontend
npm run dev
```

**Connection String in appsettings.Development.json:**
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

**Why this works:**
- Infrastructure runs in Docker (isolated)
- Your services run locally (fast debugging)
- Services connect to containerized MongoDB/Kafka via `localhost`
- Hot reload with `dotnet watch run`

### Scenario 2: Full Kubernetes with Rancher Desktop

**Best for:** Testing production-like setup

```powershell
# 1. Ensure Rancher Desktop is running with Kubernetes enabled

# 2. Build and deploy everything
skaffold dev --profile=local

# This will:
# - Build Docker images for all services
# - Push images to Rancher Desktop's registry
# - Deploy to Kubernetes cluster
# - Set up port forwarding
# - Watch for code changes and rebuild
```

**Access services:**
- Frontend: http://localhost:3000
- API Gateway: http://localhost:8080
- Grafana: http://localhost:3001

---

## Using MongoDB Container

### Quick Start

```powershell
# Start MongoDB container
docker run -d \
  --name erp-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=admin123 \
  mongo:7.0
```

**Or use docker-compose:**
```powershell
cd infrastructure
docker-compose up -d mongodb
```

### Connect from Your Application

**In appsettings.Development.json:**
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:admin123@localhost:27017",
    "DatabaseName": "erp_users"
  }
}
```

### MongoDB Commands

```powershell
# Connect to MongoDB shell
docker exec -it erp-mongodb mongosh -u admin -p admin123

# In the MongoDB shell:
show dbs                    # List all databases
use erp_users               # Switch to erp_users database
show collections            # List collections
db.users.find()             # Query users collection
db.users.countDocuments()   # Count documents

# Exit shell
exit
```

### View MongoDB Logs

```powershell
docker logs erp-mongodb -f
```

### Backup MongoDB

```powershell
# Create backup
docker exec erp-mongodb mongodump \
  --username admin \
  --password admin123 \
  --out /backup

# Restore backup
docker exec erp-mongodb mongorestore \
  --username admin \
  --password admin123 \
  /backup
```

---

## Kubernetes Manifests Explained

### What's in a Manifest?

Your manifests define **4 main resource types**:

#### 1. **Namespace** (`namespace.yaml`)
Creates an isolated environment for your resources.

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: erp-local
```

**Purpose:** Groups all your resources together. Like a folder for your apps.

#### 2. **Deployment** (e.g., `user-management.yaml`)
Defines how to run your application container.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: user-management
  namespace: erp-local
spec:
  replicas: 1                    # How many copies to run
  selector:
    matchLabels:
      app: user-management
  template:
    spec:
      containers:
      - name: user-management
        image: erp/user-management  # Docker image
        ports:
        - containerPort: 8080      # Port your app listens on
        env:                       # Environment variables
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        - name: MongoDB__ConnectionString
          valueFrom:
            secretKeyRef:          # Get from Secret
              name: mongodb-secret
              key: connection-string
```

**Purpose:** Manages your application pods. Ensures they're always running.

#### 3. **Service** (within manifest files)
Exposes your deployment to the network.

```yaml
apiVersion: v1
kind: Service
metadata:
  name: user-management-service
  namespace: erp-local
spec:
  selector:
    app: user-management
  ports:
  - port: 8080              # Port other services use
    targetPort: 8080        # Port your container listens on
```

**Purpose:** Provides a stable network endpoint. Like DNS for your app.

#### 4. **Secret** (e.g., in `mongodb.yaml`)
Stores sensitive configuration.

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: mongodb-secret
  namespace: erp-local
type: Opaque
stringData:
  username: admin
  password: admin123
  connection-string: mongodb://admin:admin123@mongodb-service:27017
```

**Purpose:** Securely stores passwords, tokens, etc.

### How Services Communicate

```
┌─────────────────────────────────────────────────────────┐
│                     Kubernetes Cluster                  │
│                                                         │
│  ┌──────────────────────────────────────────────────┐  │
│  │          Namespace: erp-local                    │  │
│  │                                                  │  │
│  │  ┌────────────────────┐                         │  │
│  │  │  gateway           │                         │  │
│  │  │  (Pod)             │                         │  │
│  │  └─────────┬──────────┘                         │  │
│  │            │ Calls http://user-management-       │  │
│  │            │        service:8080                 │  │
│  │            ▼                                     │  │
│  │  ┌────────────────────┐                         │  │
│  │  │  user-management   │                         │  │
│  │  │  (Pod)             │──┐                      │  │
│  │  └────────────────────┘  │ Connects to          │  │
│  │                           │ mongodb-service      │  │
│  │                           ▼                      │  │
│  │  ┌────────────────────┐                         │  │
│  │  │  mongodb           │                         │  │
│  │  │  (StatefulSet)     │                         │  │
│  │  └────────────────────┘                         │  │
│  │                                                  │  │
│  └──────────────────────────────────────────────────┘  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

**Key Point:** Services find each other by service name (DNS). No hardcoded IPs needed!

---

## Available Tasks

Use these tasks in VS Code (Ctrl+Shift+P → "Tasks: Run Task"):

### Infrastructure Management

| Task | Command | Description |
|------|---------|-------------|
| `infrastructure-up` | `docker-compose up -d` | Start all infrastructure containers |
| `infrastructure-down` | `docker-compose down` | Stop all infrastructure |
| `infrastructure-logs` | `docker-compose logs -f` | View all container logs |
| `mongodb-shell` | `docker exec -it erp-mongodb mongosh` | Open MongoDB shell |
| `kafka-topics-list` | List Kafka topics | View message topics |

### Kubernetes Tasks

| Task | Command | Description |
|------|---------|-------------|
| `k8s-deploy-local` | `kubectl apply -k` | Deploy manifests to local cluster |
| `k8s-delete-local` | `kubectl delete namespace` | Remove all resources |
| `k8s-get-pods` | `kubectl get pods` | List all running pods |
| `k8s-get-services` | `kubectl get services` | List all services |
| `k8s-logs-all` | `kubectl logs -f` | View logs from all pods |
| `skaffold-dev` | `skaffold dev` | Build & deploy with hot reload |
| `skaffold-stop` | `skaffold delete` | Stop Skaffold and clean up |

---

## Common Questions

### Q: Do I need Dockerfiles for Kubernetes?

**Yes!** Kubernetes runs Docker containers. The workflow is:

1. **Dockerfile** → Builds Docker image (packages your app)
2. **Docker Image** → Stored in registry (Rancher Desktop's internal registry)
3. **Kubernetes Manifest** → Tells Kubernetes which image to run

### Q: Can I use Docker Compose in production?

**Not recommended.** Docker Compose is for single-machine deployments. Use Kubernetes for:
- Multi-server clusters
- Auto-scaling
- Self-healing (automatic restarts)
- Load balancing
- Rolling updates

### Q: What's the difference between a Pod and a Container?

- **Container:** A single running instance of a Docker image
- **Pod:** The smallest Kubernetes unit; can contain 1+ containers that share networking

**Analogy:** A container is like a process. A pod is like a computer running processes.

### Q: How does Skaffold help?

Skaffold automates:
1. Building Docker images
2. Pushing to registry
3. Deploying to Kubernetes
4. Setting up port forwarding
5. Watching for code changes (hot reload)

Without Skaffold, you'd do each step manually.

---

## Quick Reference

### Development Workflow

```powershell
# Simple approach (Docker Compose)
cd infrastructure
docker-compose up -d
cd ../services/user-management/UserManagement
dotnet watch run

# Full Kubernetes approach
skaffold dev --profile=local
```

### Debugging

```powershell
# Check if containers are running
docker ps

# View container logs
docker logs erp-mongodb -f

# Check Kubernetes pods
kubectl get pods -n erp-local

# View pod logs
kubectl logs -n erp-local user-management-xxxxx -f

# Describe pod (detailed info)
kubectl describe pod -n erp-local user-management-xxxxx
```

### Access Services

**Docker Compose:**
- MongoDB: localhost:27017
- Kafka: localhost:9092
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3001

**Kubernetes (with port forwarding):**
- Frontend: http://localhost:3000
- API Gateway: http://localhost:8080
- Grafana: http://localhost:3001

---

## Next Steps

1. **Start simple:** Use Docker Compose for local development
2. **Learn kubectl:** Practice Kubernetes commands
3. **Try Skaffold:** Experience full automation
4. **Explore manifests:** Understand each YAML file
5. **Monitor:** Use Prometheus/Grafana to see metrics

Happy coding! 🚀
