# Production Deployment Guide

## Overview

Your server already has nginx running on port 80 (for WordPress) and Portainer on port 9000. The ERP system now runs on **internal ports** and nginx acts as a reverse proxy to route `shopping-now.net` to your ERP application.

## Port Mapping

**ERP Services (Internal):**
- Frontend: `localhost:8081` → Exposed as `https://shopping-now.net/`
- API Gateway: `localhost:8080` → Exposed as `https://shopping-now.net/api/`
- Kafka UI: `localhost:9001` (optional: can expose via `/kafka-ui/`)
- Grafana: `localhost:3001` (optional: can expose via `/monitoring/`)

**Existing Services:**
- Portainer: `localhost:9000` → `https://your-server:9443`
- WordPress: Served by nginx on default site
- Filebrowser: `localhost:32768`

## Deployment Commands

### Full Deployment
```bash
./deploy.sh deploy
```

### Deploy Specific Services Only
```bash
# Deploy only frontend
./deploy.sh deploy --services "frontend"

# Deploy gateway and frontend
./deploy.sh deploy --services "gateway frontend"

# Deploy infrastructure only
./deploy.sh deploy --services "mongodb kafka prometheus"
```

### View Logs
```bash
# All logs
./deploy.sh logs

# Specific service logs
./deploy.sh logs --services "frontend"
./deploy.sh logs --services "gateway"
```

### Other Commands
```bash
./deploy.sh sync          # Sync files only
./deploy.sh start         # Start containers only
./deploy.sh check         # Check status
./deploy.sh ssh           # SSH to server
```

## Setup nginx Reverse Proxy

After deploying containers, setup nginx:

```bash
# Deploy first
./deploy.sh deploy

# SSH to server
ssh ${DEPLOY_SERVER}

# Run nginx setup (as root)
cd /home/daniel/ERPDemo
sudo ./setup-nginx-proxy.sh
```

This will:
1. Install nginx and certbot (if needed)
2. Configure reverse proxy for shopping-now.net
3. Obtain SSL certificate from Let's Encrypt
4. Setup auto-renewal

## Important Notes

### docker-compose.override.yml
- **NOT deployed to production** (excluded in deploy.sh)
- Only used for local development with dummy SMTP values
- Production should use proper `.env` file on server

### Port Conflicts Resolved
- ✅ Frontend: Changed from port 80 → 8081 (nginx proxies shopping-now.net → 8081)
- ✅ Kafka UI: Changed from port 9000 → 9001 (Portainer uses 9000)

### Environment Variables
On the server, create `/home/daniel/ERPDemo/.env`:
```bash
# Production values
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-production-email@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_FROM=noreply@shopping-now.net

JWT_SECRET=your-strong-production-secret
```

## Troubleshooting

### Check Container Status
```bash
ssh ${DEPLOY_SERVER}
cd /home/daniel/ERPDemo
docker-compose ps
```

### View Logs
```bash
docker-compose logs -f frontend
docker-compose logs -f gateway
```

### Check nginx Configuration
```bash
sudo nginx -t
sudo systemctl status nginx
```

### Check SSL Certificate
```bash
sudo certbot certificates
sudo certbot renew --dry-run
```

### Restart Services
```bash
# Restart specific service
docker-compose restart frontend

# Restart all
docker-compose restart

# Rebuild and restart
docker-compose up -d --build frontend
```

## DNS Configuration

Make sure `shopping-now.net` points to your server IP:
```
A record: shopping-now.net → your-server-ip
A record: www.shopping-now.net → your-server-ip
```

## Security Checklist

- [ ] SSL certificate installed and auto-renewing
- [ ] Production `.env` file with strong secrets
- [ ] Firewall configured (allow 80, 443, 22 only)
- [ ] MongoDB not exposed (only accessible internally)
- [ ] Kafka not exposed publicly
- [ ] Prometheus not exposed publicly (or behind auth)

## Quick Commands

```bash
# Deploy everything
./deploy.sh deploy

# Deploy only frontend changes
./deploy.sh deploy --services "frontend"

# View frontend logs
./deploy.sh logs --services "frontend"

# SSH to server
./deploy.sh ssh

# Check deployment status
./deploy.sh check
```
