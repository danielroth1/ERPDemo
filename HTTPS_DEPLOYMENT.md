# HTTPS & Production Deployment Guide

## 🌐 Domain: shopping-now.net

### Current Configuration

**Ports:**
- HTTP: Port 80 (redirects to HTTPS)
- HTTPS: Port 443 (main traffic)
- Dev: Port 3000 (local development)

**Services:**
- Frontend: `https://shopping-now.net`
- API Gateway: `https://shopping-now.net/api`

---

## 📋 Quick Start

### Local Development (Current Machine)

The Loki mount error is fixed. Run:
```bash
cd /Users/daniel/Projects/ERPDemo
docker-compose down
docker-compose up -d
```

### Production Server Setup

1. **SSH to your server:**
   ```bash
   ssh ${DEPLOY_SERVER}
   ```

2. **Clone/Deploy your code:**
   ```bash
   cd /path/to/ERPDemo
   ```

3. **Run HTTPS setup:**
   ```bash
   sudo ./setup-https.sh
   ```

4. **Update docker-compose.yml on server** - Add SSL volume:
   ```yaml
   frontend:
     volumes:
       - ./ssl-certs:/etc/nginx/ssl:ro
   ```

5. **Start containers:**
   ```bash
   docker-compose up -d
   ```

---

## 🔧 Container Management

### Managing Outdated/Running Containers

Use the provided management script:
```bash
./manage-containers.sh
```

Or manually:

**Stop all containers:**
```bash
docker-compose down
```

**Remove and rebuild all:**
```bash
docker-compose down --rmi all -v
docker-compose up -d --build
```

**Update specific service:**
```bash
docker-compose up -d --build --force-recreate frontend
```

**Remove orphaned containers:**
```bash
docker-compose down --remove-orphans
```

**Full cleanup (nuclear option):**
```bash
docker-compose down --rmi all -v --remove-orphans
docker system prune -af --volumes
```

---

## 🔐 SSL Certificate Setup (Let's Encrypt)

### Option 1: Automated Script (Recommended)
```bash
sudo ./setup-https.sh
```

### Option 2: Manual Setup

1. **Install Certbot:**
   ```bash
   sudo apt-get update
   sudo apt-get install certbot
   ```

2. **Stop nginx (if running):**
   ```bash
   docker-compose stop frontend
   ```

3. **Obtain certificate:**
   ```bash
   sudo certbot certonly --standalone \
     -d shopping-now.net \
     -d www.shopping-now.net \
     --non-interactive \
     --agree-tos \
     --email admin@shopping-now.net
   ```

4. **Copy certificates:**
   ```bash
   mkdir -p ssl-certs
   sudo cp /etc/letsencrypt/live/shopping-now.net/fullchain.pem ssl-certs/
   sudo cp /etc/letsencrypt/live/shopping-now.net/privkey.pem ssl-certs/
   sudo chmod 644 ssl-certs/fullchain.pem
   sudo chmod 600 ssl-certs/privkey.pem
   ```

5. **Add volume mount to docker-compose.yml:**
   ```yaml
   frontend:
     # ... other config ...
     volumes:
       - ./ssl-certs:/etc/nginx/ssl:ro
   ```

6. **Restart frontend:**
   ```bash
   docker-compose up -d --build frontend
   ```

### Option 3: Without SSL (Development)

If you want to temporarily run without SSL:

1. Edit `frontend/nginx.conf` and comment out SSL lines:
   ```nginx
   # ssl_certificate /etc/nginx/ssl/fullchain.pem;
   # ssl_certificate_key /etc/nginx/ssl/privkey.pem;
   ```

2. Or just use HTTP on port 80

---

## 🔄 Certificate Renewal

Certbot automatically renews certificates. To test:
```bash
sudo certbot renew --dry-run
```

After renewal, copy new certificates and restart:
```bash
sudo cp /etc/letsencrypt/live/shopping-now.net/*.pem ssl-certs/
docker-compose restart frontend
```

Or add a cron job:
```bash
sudo crontab -e
# Add this line:
0 0 * * 0 certbot renew --quiet && cp /etc/letsencrypt/live/shopping-now.net/*.pem /path/to/ERPDemo/ssl-certs/ && cd /path/to/ERPDemo && docker-compose restart frontend
```

---

## 🐛 Troubleshooting

### Issue: Loki mount error
**Fixed!** The mount path was corrected from `/etc/loki/local-config.yaml` to `/etc/loki/config.yaml`

### Issue: Containers not running on server

**Check status:**
```bash
docker-compose ps
```

**View logs:**
```bash
docker-compose logs frontend
docker-compose logs gateway
```

**Rebuild non-running containers:**
```bash
docker-compose up -d --build frontend gateway grafana loki prometheus kafka-ui
```

### Issue: Port conflicts

If port 80/443 is already in use:
```bash
# Check what's using the port
sudo lsof -i :80
sudo lsof -i :443

# Stop the conflicting service
sudo systemctl stop nginx  # if system nginx is running
```

### Issue: Permission denied for SSL certificates

```bash
sudo chown -R $(whoami):$(whoami) ssl-certs/
chmod 644 ssl-certs/fullchain.pem
chmod 600 ssl-certs/privkey.pem
```

### Issue: DNS not pointing to server

Update your DNS records:
- **A Record:** `shopping-now.net` → `Your Server IP`
- **A Record:** `www.shopping-now.net` → `Your Server IP`

Check DNS propagation:
```bash
nslookup shopping-now.net
dig shopping-now.net
```

---

## 📊 Verify Setup

After deployment, check:

1. **HTTPS works:**
   ```bash
   curl -I https://shopping-now.net
   ```

2. **HTTP redirects:**
   ```bash
   curl -I http://shopping-now.net
   ```

3. **API accessible:**
   ```bash
   curl https://shopping-now.net/api/health
   ```

4. **All containers running:**
   ```bash
   docker-compose ps
   ```

---

## 🚀 Production Checklist

- [ ] DNS A records point to server IP
- [ ] SSL certificates obtained and mounted
- [ ] Firewall allows ports 80, 443
- [ ] docker-compose.yml updated with production values
- [ ] .env file created with production secrets
- [ ] All containers running (`docker-compose ps`)
- [ ] HTTPS redirect working
- [ ] API gateway accessible via /api
- [ ] Database backed up
- [ ] Monitoring/logging configured

---

## 📁 File Structure

```
ERPDemo/
├── ssl-certs/                    # SSL certificates (gitignored)
│   ├── fullchain.pem
│   └── privkey.pem
├── frontend/
│   └── nginx.conf                # ✅ Now with HTTPS support
├── docker-compose.yml            # ✅ Fixed Loki mount, added HTTPS ports
├── docker-compose.override.yml   # Local dev overrides
├── .env.example                  # Template for environment variables
├── .env                          # Your secrets (create this)
├── setup-https.sh               # 🆕 SSL setup script
└── manage-containers.sh          # 🆕 Container management script
```

---

## 🔗 Useful Commands

```bash
# View all container logs
docker-compose logs -f

# Restart specific service
docker-compose restart frontend

# Scale a service (not applicable for single instance)
docker-compose up -d --scale frontend=2

# Execute command in container
docker-compose exec frontend sh

# View resource usage
docker stats

# Remove unused data
docker system prune -a
```
