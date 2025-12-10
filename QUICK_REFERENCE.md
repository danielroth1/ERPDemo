# Quick Reference - HTTPS Setup for shopping-now.net

## ✅ What Was Fixed

1. **Loki Mount Error** - Changed from `/etc/loki/local-config.yaml` to `/etc/loki/config.yaml`
2. **HTTPS Support** - Updated nginx.conf with SSL configuration
3. **Port Configuration** - Added ports 80, 443 for HTTPS
4. **Domain Configuration** - Set up for shopping-now.net

## 🚀 Deploy to Server (shopping-now.net)

### Step 1: Connect to Server
```bash
ssh ${DEPLOY_SERVER}
```

### Step 2: Deploy Code
```bash
cd /path/to/ERPDemo
git pull  # or upload files
```

### Step 3: Setup SSL
```bash
sudo ./setup-https.sh
```

### Step 4: Update docker-compose.yml
Add this to the frontend service:
```yaml
frontend:
  volumes:
    - ./ssl-certs:/etc/nginx/ssl:ro
```

### Step 5: Start
```bash
docker-compose up -d
```

## 🔧 Container Management Commands

### Fix Already Running Containers
```bash
# Option 1: Stop and restart
docker-compose down
docker-compose up -d

# Option 2: Force recreate
docker-compose up -d --force-recreate

# Option 3: Rebuild outdated containers
docker-compose up -d --build

# Option 4: Interactive menu
./manage-containers.sh
```

### Replace/Update Specific Service
```bash
docker-compose up -d --build --force-recreate frontend
docker-compose up -d --build --force-recreate gateway
```

### Clean Everything and Start Fresh
```bash
docker-compose down --rmi all -v --remove-orphans
docker-compose up -d --build
```

## 📊 Port Configuration

### Development (localhost)
- Frontend: http://localhost:3000
- API Gateway: http://localhost:8080
- Grafana: http://localhost:3001
- Prometheus: http://localhost:9090
- MongoDB: localhost:27017
- Kafka: localhost:9092

### Production (shopping-now.net)
- Frontend: https://shopping-now.net (port 443)
- HTTP Redirect: http://shopping-now.net (port 80 → 443)
- API: https://shopping-now.net/api
- All backend services: Internal network only

## 🔍 Check Status

### View All Containers
```bash
docker-compose ps
```

### View Logs
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f frontend
docker-compose logs -f gateway

# Last 50 lines
docker-compose logs --tail=50 frontend
```

### Check Which Containers Are NOT Running
```bash
docker-compose ps | grep -v "Up"
```

### Restart Failed Containers
```bash
# Get service names that aren't running
FAILED=$(docker-compose ps --filter "status=exited" --services)

# Restart them
for service in $FAILED; do
  docker-compose up -d --build $service
done
```

## 🐛 Troubleshoot Server Containers

### SSH to Server
```bash
ssh ${DEPLOY_SERVER}
```

### Check Why Containers Failed
```bash
cd /path/to/ERPDemo

# View status
docker-compose ps

# Check logs of failed containers
docker-compose logs frontend
docker-compose logs gateway
docker-compose logs grafana

# Try to start specific container
docker-compose up frontend
# (without -d to see errors in real-time)
```

### Common Issues on Server

**Frontend not running:**
```bash
# Check if port 80/443 is available
sudo lsof -i :80
sudo lsof -i :443

# If system nginx is blocking:
sudo systemctl stop nginx
sudo systemctl disable nginx

# Restart frontend
docker-compose up -d --build frontend
```

**Gateway not running:**
```bash
docker-compose logs gateway
docker-compose up -d --build gateway
```

**Grafana/Prometheus/Loki not running:**
```bash
# Check config files exist
ls -la infrastructure/monitoring/prometheus/
ls -la infrastructure/logging/

# Restart monitoring stack
docker-compose up -d grafana prometheus loki
```

## 📝 Files Modified

- ✅ `docker-compose.yml` - Fixed Loki mount, added HTTPS ports
- ✅ `frontend/nginx.conf` - Added HTTPS config for shopping-now.net
- ✅ `docker-compose.override.yml` - Already has SMTP settings
- 🆕 `setup-https.sh` - Automated SSL certificate setup
- 🆕 `manage-containers.sh` - Interactive container management
- 🆕 `HTTPS_DEPLOYMENT.md` - Complete deployment guide

## 🎯 Next Steps

1. ✅ Local containers fixed - all running
2. ⏳ Deploy to server: `ssh ${DEPLOY_SERVER}`
3. ⏳ Run SSL setup: `sudo ./setup-https.sh`
4. ⏳ Start containers: `docker-compose up -d`
5. ⏳ Verify: `https://shopping-now.net`

---

**Need Help?** Check `HTTPS_DEPLOYMENT.md` for detailed instructions.
