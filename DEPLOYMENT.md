# ERP System - Deployment Guide

## Quick Start (5 Minutes)

### Prerequisites
- Docker Desktop or Rancher Desktop
- Node.js 18+ and npm
- .NET 9 SDK
- 8GB RAM minimum

### 1. Start Backend Services

```bash
# Start all backend services with Docker Compose
cd infrastructure
docker-compose up -d

# Wait for services to be healthy (~2 minutes)
docker-compose ps
```

**Services Started:**
- MongoDB (Ports: 27017, 27018, 27019)
- Kafka + Zookeeper (Port: 9092)
- Prometheus (Port: 9090)
- Grafana (Port: 3000)
- User Management API (Port: 5001)
- Inventory API (Port: 5002)
- Sales API (Port: 5003)
- Financial API (Port: 5004)
- Analytics API (Port: 5005)
- API Gateway (Port: 5000)

### 2. Start Frontend

```bash
# Navigate to frontend
cd ../frontend

# Install dependencies (first time only)
npm install

# Start development server
npm run dev
```

**Access Application:**
- Frontend: http://localhost:5173
- API Gateway: http://localhost:5000
- Swagger UI: http://localhost:5001/swagger (User Management)

### 3. Test the System

```bash
# Create test user
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@erp.com",
    "password": "Admin123!",
    "firstName": "Admin",
    "lastName": "User"
  }'

# Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@erp.com",
    "password": "Admin123!"
  }'
```

## Production Deployment

### Option 1: Kubernetes (Recommended)

```bash
# Apply all Kubernetes manifests
kubectl apply -f kubernetes/

# Check deployment status
kubectl get pods -n erp-system

# Get service URLs
kubectl get services -n erp-system
```

**Kubernetes Resources:**
- 6 backend service deployments
- 3 MongoDB StatefulSets
- 1 Kafka StatefulSet
- Frontend Nginx deployment
- API Gateway LoadBalancer
- Prometheus & Grafana deployments

### Option 2: Docker Compose (Simple)

```bash
# Production build
cd services/user-management
dotnet publish -c Release

cd ../../frontend
npm run build

# Start with production compose file
cd ../infrastructure
docker-compose -f docker-compose.prod.yml up -d
```

### Option 3: Manual Deployment

```bash
# Build all backend services
for service in services/*; do
  cd $service
  dotnet publish -c Release -o ./publish
  cd ../..
done

# Build frontend
cd frontend
npm run build
# Deploy dist/ folder to Nginx/IIS

# Configure reverse proxy (Nginx example)
# See nginx.conf in infrastructure/
```

## Environment Configuration

### Backend Services

Create `.env` file or configure environment variables:

```bash
# MongoDB
MONGODB_CONNECTION_STRING=mongodb://localhost:27017
MONGODB_DATABASE_NAME=erp_users

# Kafka
KAFKA_BOOTSTRAP_SERVERS=localhost:9092

# JWT
JWT_SECRET=your-secret-key-change-in-production
JWT_ISSUER=erp-system
JWT_AUDIENCE=erp-client

# Email (Optional)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
```

### Frontend

Create `frontend/.env.production`:

```bash
VITE_API_GATEWAY_URL=https://api.yourdomain.com
VITE_WEBSOCKET_URL=wss://api.yourdomain.com/hubs
```

## Monitoring & Observability

### Prometheus Metrics

**Access:** http://localhost:9090

**Key Metrics:**
- `http_requests_total` - Total HTTP requests
- `http_request_duration_seconds` - Request latency
- `dotnet_total_memory_bytes` - Memory usage
- `process_cpu_seconds_total` - CPU usage

### Grafana Dashboards

**Access:** http://localhost:3000  
**Default Credentials:** admin/admin

**Pre-configured Dashboards:**
1. **ERP Overview** - System-wide metrics
2. **Service Health** - Individual service status
3. **Database Performance** - MongoDB metrics
4. **API Gateway** - Request routing and errors

### Log Aggregation

```bash
# View logs from all services
docker-compose logs -f

# View specific service logs
docker-compose logs -f user-management

# Kubernetes logs
kubectl logs -f deployment/user-management -n erp-system
```

## Security Checklist

### Before Production:

- [ ] Change default JWT secret key
- [ ] Enable HTTPS/TLS certificates
- [ ] Configure MongoDB authentication
- [ ] Set up Kafka SASL/SSL
- [ ] Enable rate limiting on API Gateway
- [ ] Configure CORS for production domains
- [ ] Set up firewall rules
- [ ] Enable container security scanning
- [ ] Configure secret management (Azure Key Vault, AWS Secrets Manager)
- [ ] Set up backup automation

### Recommended Security Headers:

```nginx
add_header X-Frame-Options "SAMEORIGIN";
add_header X-Content-Type-Options "nosniff";
add_header X-XSS-Protection "1; mode=block";
add_header Referrer-Policy "strict-origin-when-cross-origin";
add_header Content-Security-Policy "default-src 'self'";
```

## Performance Tuning

### Database Optimization

```javascript
// MongoDB indexes (run on each database)
db.users.createIndex({ "email": 1 }, { unique: true });
db.products.createIndex({ "sku": 1 }, { unique: true });
db.products.createIndex({ "stockQuantity": 1, "reorderLevel": 1 });
db.orders.createIndex({ "customerId": 1, "orderDate": -1 });
db.transactions.createIndex({ "date": -1 });
```

### API Gateway Caching

Edit `gateway/appsettings.json`:

```json
{
  "Caching": {
    "Enabled": true,
    "DefaultTTL": 60,
    "Routes": {
      "/api/v1/products": 300,
      "/api/v1/categories": 600
    }
  }
}
```

### Frontend Optimization

```bash
# Enable code splitting
npm run build -- --mode production

# Analyze bundle size
npm run build -- --mode analyze

# Enable compression
npm install -D vite-plugin-compression
```

## Scaling Strategies

### Horizontal Scaling

```bash
# Scale backend services
docker-compose up -d --scale user-management=3
docker-compose up -d --scale inventory-management=3

# Kubernetes autoscaling
kubectl autoscale deployment user-management \
  --cpu-percent=70 \
  --min=2 \
  --max=10 \
  -n erp-system
```

### Database Scaling

**MongoDB Replica Set:**

```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: mongodb
spec:
  replicas: 3
  serviceName: mongodb
  template:
    spec:
      containers:
      - name: mongodb
        image: mongo:7.0
        command:
          - mongod
          - --replSet
          - rs0
```

**Kafka Cluster:**

```yaml
replicas: 3  # Increase brokers
resources:
  requests:
    memory: "2Gi"
    cpu: "1000m"
```

## Health Checks

### Service Health Endpoints

```bash
# Check all services
curl http://localhost:5000/health  # API Gateway
curl http://localhost:5001/health  # User Management
curl http://localhost:5002/health  # Inventory
curl http://localhost:5003/health  # Sales
curl http://localhost:5004/health  # Financial
curl http://localhost:5005/health  # Analytics

# Kubernetes health checks
kubectl get pods -n erp-system
kubectl describe pod <pod-name> -n erp-system
```

### Database Health

```bash
# MongoDB
docker exec -it mongodb-user mongo --eval "db.adminCommand('ping')"

# Kafka
docker exec -it kafka kafka-topics.sh --bootstrap-server localhost:9092 --list
```

## Backup & Recovery

### Database Backup

```bash
# MongoDB backup
docker exec mongodb-user mongodump \
  --db=erp_users \
  --out=/backup/$(date +%Y%m%d)

# Restore
docker exec mongodb-user mongorestore \
  --db=erp_users \
  /backup/20241203
```

### Application State Backup

```bash
# Backup Kubernetes volumes
kubectl get pvc -n erp-system
velero backup create erp-backup --include-namespaces erp-system

# Restore
velero restore create --from-backup erp-backup
```

## Troubleshooting

### Common Issues

**1. Service Won't Start**
```bash
# Check logs
docker-compose logs service-name

# Check port conflicts
netstat -ano | findstr :5001

# Restart service
docker-compose restart service-name
```

**2. MongoDB Connection Failed**
```bash
# Check MongoDB status
docker exec -it mongodb-user mongo --eval "db.adminCommand('ping')"

# Check connection string
echo $MONGODB_CONNECTION_STRING

# Test connectivity
telnet localhost 27017
```

**3. Frontend Can't Connect to API**
```bash
# Check API Gateway
curl http://localhost:5000/health

# Check CORS settings
# Edit gateway/appsettings.json

# Check environment variables
cat frontend/.env
```

**4. High Memory Usage**
```bash
# Check resource usage
docker stats

# Set memory limits
docker-compose.yml:
  deploy:
    resources:
      limits:
        memory: 512M
```

### Debug Mode

```bash
# Enable debug logging
export ASPNETCORE_ENVIRONMENT=Development
export LOGGING__LOGLEVEL__DEFAULT=Debug

# Frontend debug
npm run dev -- --debug
```

## Performance Benchmarks

### Expected Performance

- **API Response Time**: < 100ms (95th percentile)
- **Database Queries**: < 50ms average
- **Frontend Load Time**: < 2 seconds (First Contentful Paint)
- **WebSocket Latency**: < 50ms
- **Throughput**: 1000+ requests/second per service

### Load Testing

```bash
# Install k6
choco install k6

# Run load test
k6 run loadtest/api-test.js

# Artillery alternative
npm install -g artillery
artillery run loadtest/scenarios.yml
```

## Support & Maintenance

### Regular Tasks

**Daily:**
- Monitor service health dashboards
- Check error logs
- Review system alerts

**Weekly:**
- Analyze performance metrics
- Review database query performance
- Update dependencies

**Monthly:**
- Security patching
- Database optimization
- Capacity planning review

### Monitoring Alerts

Configure Prometheus alerts:

```yaml
groups:
  - name: erp_alerts
    rules:
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        annotations:
          summary: "High error rate detected"
      
      - alert: ServiceDown
        expr: up{job="user-management"} == 0
        for: 2m
        annotations:
          summary: "Service is down"
```

## Additional Resources

- **API Documentation**: http://localhost:5001/swagger
- **Grafana Dashboards**: http://localhost:3000
- **Prometheus Metrics**: http://localhost:9090
- **GitHub Repository**: (your repo URL)
- **Support Email**: support@yourdomain.com

## Version History

- **v1.0.0** (Dec 2024) - Initial release
  - 6 microservices
  - React 19 frontend
  - Complete CRUD operations
  - Real-time updates
  - Monitoring stack

---

**Deployment Time Estimates:**
- Local Development: 5 minutes
- Docker Compose Production: 15 minutes
- Kubernetes Production: 30 minutes
- Full Production with Monitoring: 2 hours
