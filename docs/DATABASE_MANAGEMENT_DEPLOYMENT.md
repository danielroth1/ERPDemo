# Database Management Service - Deployment Checklist

## Pre-Deployment

### Infrastructure Requirements
- [ ] Redis service configured and accessible
- [ ] MongoDB accessible from Dashboard service
- [ ] Network connectivity between services verified
- [ ] SSL/TLS certificates for production (if needed)

### Configuration
- [ ] Update `appsettings.json` with production Redis connection
- [ ] Verify MongoDB connection strings
- [ ] Set appropriate cache TTL values
- [ ] Configure JWT settings for production

### Security Review
- [ ] Verify role-based access control (Admin/Manager only)
- [ ] Review query validation keywords
- [ ] Check result limits are appropriate
- [ ] Audit logging is enabled
- [ ] API endpoints are properly secured

## Deployment Steps

### 1. Backend Deployment

```powershell
# Build the Dashboard service
cd services/dashboard/DashboardAnalytics
dotnet restore
dotnet build -c Release

# Run tests (if available)
dotnet test

# Publish
dotnet publish -c Release -o ./publish
```

### 2. Database Setup

```powershell
# Ensure MongoDB collections exist
# Collections are auto-created on first use:
# - query_executions
# - database_alerts
```

### 3. Redis Setup

```powershell
# Verify Redis is running
docker ps | grep redis

# Test Redis connection
docker exec erp-redis redis-cli ping
# Expected output: PONG

# Check Redis config
docker exec erp-redis redis-cli CONFIG GET maxmemory
```

### 4. Frontend Deployment

```powershell
# Build frontend with database management
cd frontend
npm install
npm run build

# Output in: frontend/dist
```

### 5. Start Services

```powershell
# Start Dashboard service
cd services/dashboard/DashboardAnalytics
dotnet run --environment Production

# Or using Docker
docker-compose up -d dashboard
```

## Post-Deployment Verification

### Health Checks

- [ ] Dashboard service health endpoint: `GET /health/live`
- [ ] Dashboard service ready endpoint: `GET /health/ready`
- [ ] GraphQL endpoint accessible: `GET /graphql`
- [ ] REST API endpoints responding
- [ ] WebSocket connection working

### Functional Tests

#### 1. Database Overview
```bash
# Test database overview endpoint
curl -X GET "http://localhost:5005/api/v1/database/overview" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: 200 OK with database stats
```

#### 2. Service Database
```bash
# Test specific service database
curl -X GET "http://localhost:5005/api/v1/database/service/Inventory" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: 200 OK with Inventory database info
```

#### 3. Search
```bash
# Test database search
curl -X POST "http://localhost:5005/api/v1/database/search" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"searchTerm": "products"}'

# Expected: 200 OK with search results
```

#### 4. Cache
```bash
# Test cache functionality
curl -X POST "http://localhost:5005/api/v1/database/cache/clear" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: 200 OK with success message
```

#### 5. Query Execution (Admin)
```bash
# Test query execution
curl -X POST "http://localhost:5005/api/v1/database/query" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "databaseName": "erp_inventory",
    "collectionName": "products",
    "query": "{}",
    "queryType": "Count",
    "limit": 1
  }'

# Expected: 200 OK with query results
```

#### 6. Alerts
```bash
# Test alerts endpoint
curl -X GET "http://localhost:5005/api/v1/database/alerts" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Expected: 200 OK with alerts list
```

#### 7. GraphQL
```bash
# Test GraphQL query
curl -X POST "http://localhost:5005/graphql" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "query { getDatabaseOverview { id generatedAt totalStats { totalCollections } } }"
  }'

# Expected: 200 OK with GraphQL response
```

### Frontend Verification

- [ ] Navigate to `/database` page
- [ ] Overview tab loads without errors
- [ ] Service cards expand/collapse correctly
- [ ] Search tab functional
- [ ] Query tab accessible (Admin only)
- [ ] Alerts tab displays correctly
- [ ] WebSocket connection indicator shows "Live"
- [ ] Auto-refresh toggle works
- [ ] Cache clear button functional

### Performance Tests

#### Cache Performance
```bash
# First request (cache miss)
time curl -X GET "http://localhost:5005/api/v1/database/overview" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Second request (cache hit - should be faster)
time curl -X GET "http://localhost:5005/api/v1/database/overview" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Query Limits
```bash
# Test result limit enforcement
curl -X POST "http://localhost:5005/api/v1/database/query" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "databaseName": "erp_inventory",
    "collectionName": "products",
    "query": "{}",
    "queryType": "Find",
    "limit": 2000
  }'

# Expected: Limit should be capped at 1000
```

## Monitoring Setup

### Metrics to Monitor

1. **Redis Metrics**
   - Cache hit rate
   - Memory usage
   - Connection count
   - Eviction rate

2. **API Metrics**
   - Request rate per endpoint
   - Response times
   - Error rates
   - Query execution times

3. **Database Metrics**
   - Query execution count
   - Failed query count
   - Alert generation rate
   - Collection count changes

### Prometheus Metrics

```yaml
# Add to prometheus.yml
- job_name: 'dashboard-database-management'
  static_configs:
    - targets: ['dashboard:5005']
  metrics_path: '/metrics'
```

### Grafana Dashboards

Create dashboard panels for:
- Database overview request rate
- Cache hit/miss ratio
- Query execution time distribution
- Alert count by severity
- WebSocket connection count

## Troubleshooting

### Common Issues

#### Redis Connection Failed
```bash
# Check Redis is running
docker ps | grep redis

# Check logs
docker logs erp-redis

# Test connection
redis-cli -h localhost -p 6379 ping
```

#### MongoDB Connection Issues
```bash
# Check MongoDB is accessible
docker exec erp-mongodb mongosh --eval "db.adminCommand('ping')"

# Check connection from Dashboard service
docker exec dashboard-service curl mongodb:27017
```

#### WebSocket Not Connecting
1. Check browser console for WebSocket errors
2. Verify JWT token is valid
3. Check CORS settings
4. Verify WebSocket endpoint: `ws://localhost:5005/graphql`

#### Query Execution Fails
1. Check MongoDB user permissions
2. Verify collection exists
3. Review query validation rules
4. Check query execution logs

### Logs to Review

```powershell
# Dashboard service logs
docker logs dashboard-service

# Redis logs
docker logs erp-redis

# MongoDB logs
docker logs erp-mongodb

# Filter for database management
docker logs dashboard-service | grep "Database"
```

## Rollback Plan

If deployment fails:

1. **Stop Dashboard service**
   ```powershell
   docker stop dashboard-service
   ```

2. **Revert to previous version**
   ```powershell
   git checkout <previous-tag>
   dotnet build -c Release
   ```

3. **Clear Redis cache**
   ```powershell
   docker exec erp-redis redis-cli FLUSHDB
   ```

4. **Restart with previous version**
   ```powershell
   docker start dashboard-service
   ```

## Sign-Off Checklist

### Development Team
- [ ] Code review completed
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] Documentation updated
- [ ] API endpoints tested

### Operations Team
- [ ] Infrastructure provisioned
- [ ] Redis configured
- [ ] Monitoring configured
- [ ] Backup procedures verified
- [ ] Rollback plan tested

### Security Team
- [ ] Security review completed
- [ ] Authentication tested
- [ ] Authorization tested
- [ ] Query validation tested
- [ ] Audit logging verified

### Business/Product Team
- [ ] Feature acceptance tested
- [ ] User documentation reviewed
- [ ] Training materials prepared
- [ ] Support team briefed

## Post-Deployment

### Week 1
- [ ] Monitor error rates
- [ ] Review query execution logs
- [ ] Check cache hit rates
- [ ] Gather user feedback

### Week 2-4
- [ ] Analyze usage patterns
- [ ] Optimize cache TTL if needed
- [ ] Review and adjust alerts
- [ ] Plan improvements

## Support Contacts

- **Backend Issues**: [Backend Team]
- **Frontend Issues**: [Frontend Team]
- **Infrastructure**: [DevOps Team]
- **Security**: [Security Team]

## Additional Resources

- [Database Management Documentation](./DATABASE_MANAGEMENT.md)
- [Quick Start Guide](./DATABASE_MANAGEMENT_QUICKSTART.md)
- [Implementation Summary](./DATABASE_MANAGEMENT_IMPLEMENTATION.md)
