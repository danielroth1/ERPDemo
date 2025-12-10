# Production Deployment Guide

Complete guide for deploying the ERP system to a self-hosted Linux server.

## 🖥️ Server Requirements

### Minimum Specifications
- **OS**: Ubuntu 22.04 LTS or later
- **CPU**: 2 cores (4 cores recommended)
- **RAM**: 4-5 GB (8 GB recommended for production workload)
- **Storage**: 50 GB SSD
- **Network**: Static IP address, ports 80/443/22 open

### Domain & DNS Setup
- Domain name (e.g., erp.yourdomain.com)
- DNS A record pointing to server IP
- Subdomain for API (api.yourdomain.com)
- Subdomain for monitoring (grafana.yourdomain.com)

## 🛠️ Initial Server Setup

### 1. Create Deploy User

```bash
# SSH as root
ssh root@your-server-ip

# Update system
apt update && apt upgrade -y

# Create deploy user
adduser deploy
usermod -aG sudo deploy

# Configure SSH key authentication
mkdir -p /home/deploy/.ssh
cp ~/.ssh/authorized_keys /home/deploy/.ssh/
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh
chmod 600 /home/deploy/.ssh/authorized_keys

# Test login as deploy user
ssh deploy@your-server-ip
```

### 2. Configure Firewall

```bash
# Enable UFW firewall
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Allow SSH, HTTP, HTTPS
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Enable firewall
sudo ufw enable
sudo ufw status
```

### 3. Install Required Software

```bash
# Install basic tools
sudo apt install -y curl wget git unzip

# Install Docker (if not already installed)
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker deploy
newgrp docker

# Verify Docker
docker --version
docker run hello-world
```

## ☸️ Kubernetes Setup with K3s

### Install K3s

```bash
# Install K3s (lightweight Kubernetes)
curl -sfL https://get.k3s.io | sh -

# Check status
sudo systemctl status k3s

# Set up kubeconfig for deploy user
mkdir -p ~/.kube
sudo cp /etc/rancher/k3s/k3s.yaml ~/.kube/config
sudo chown deploy:deploy ~/.kube/config
export KUBECONFIG=~/.kube/config
echo 'export KUBECONFIG=~/.kube/config' >> ~/.bashrc

# Verify kubectl works
kubectl version
kubectl get nodes
```

### Install kubectl (if not included)

```bash
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
sudo install -o root -g root -m 0755 kubectl /usr/local/bin/kubectl
kubectl version --client
```

## 🔐 Install cert-manager

```bash
# Clone repository
git clone <your-repo-url> ~/erp
cd ~/erp

# Make install script executable
chmod +x infrastructure/cert-manager/install.sh

# Run installation script
./infrastructure/cert-manager/install.sh

# Verify installation
kubectl get pods -n cert-manager
kubectl get clusterissuer

# Update email in ClusterIssuer
kubectl edit clusterissuer letsencrypt-prod
# Change email: admin@yourdomain.com to your email
```

## 🕸️ Install Linkerd

```bash
# Install Linkerd CLI
curl -fsL https://run.linkerd.io/install | sh
export PATH=$PATH:$HOME/.linkerd2/bin
echo 'export PATH=$PATH:$HOME/.linkerd2/bin' >> ~/.bashrc

# Pre-flight check
linkerd check --pre

# Install Linkerd CRDs
linkerd install --crds | kubectl apply -f -

# Install Linkerd control plane (single replica for resource efficiency)
linkerd install \
  --set controlPlaneReplicas=1 \
  --set proxyInjector.replicas=1 \
  | kubectl apply -f -

# Verify installation
linkerd check

# Install Linkerd Viz
linkerd viz install | kubectl apply -f -
linkerd viz check
```

## 🗄️ Prepare Secrets

### Create Secret Files

```bash
cd ~/erp

# Generate strong JWT secret
JWT_SECRET=$(openssl rand -base64 32)
echo $JWT_SECRET

# Generate strong MongoDB password
MONGO_PASSWORD=$(openssl rand -base64 16)
echo $MONGO_PASSWORD

# Create secrets file from template
cp infrastructure/k8s/production/secrets.yaml.example infrastructure/k8s/production/secrets.yaml

# Edit secrets file
nano infrastructure/k8s/production/secrets.yaml
```

**secrets.yaml**:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: mongodb-secret
  namespace: erp-prod
type: Opaque
stringData:
  username: admin
  password: YOUR_MONGODB_PASSWORD_HERE
  connection-string: mongodb://admin:YOUR_MONGODB_PASSWORD_HERE@mongodb-service:27017
---
apiVersion: v1
kind: Secret
metadata:
  name: jwt-secret
  namespace: erp-prod
type: Opaque
stringData:
  secret: YOUR_JWT_SECRET_HERE
  issuer: erp-system
  audience: erp-clients
---
apiVersion: v1
kind: Secret
metadata:
  name: smtp-secret
  namespace: erp-prod
type: Opaque
stringData:
  host: smtp.gmail.com
  port: "587"
  username: YOUR_EMAIL@gmail.com
  password: YOUR_APP_PASSWORD
  from-email: noreply@yourdomain.com
```

### Apply Secrets

```bash
# Create production namespace
kubectl create namespace erp-prod

# Enable Linkerd injection
kubectl annotate namespace erp-prod linkerd.io/inject=enabled

# Apply secrets
kubectl apply -f infrastructure/k8s/production/secrets.yaml

# Verify secrets
kubectl get secrets -n erp-prod
```

## 🚀 Deploy Application

### Using Kubectl

```bash
cd ~/erp

# Apply base manifests
kubectl apply -f infrastructure/k8s/base/

# Apply production overlays
kubectl apply -f infrastructure/k8s/production/

# Wait for pods to be ready
kubectl get pods -n erp-prod -w

# Check deployment status
kubectl get deployments -n erp-prod
kubectl get statefulsets -n erp-prod
kubectl get services -n erp-prod
```

### Configure Ingress

Create `infrastructure/k8s/production/ingress.yaml`:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: erp-ingress
  namespace: erp-prod
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/force-ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - erp.yourdomain.com
    - api.yourdomain.com
    - grafana.yourdomain.com
    secretName: erp-tls-cert
  rules:
  - host: erp.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: frontend-service
            port:
              number: 80
  - host: api.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: gateway-service
            port:
              number: 80
  - host: grafana.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: grafana-service
            port:
              number: 3000
```

Apply ingress:
```bash
kubectl apply -f infrastructure/k8s/production/ingress.yaml

# Check ingress
kubectl get ingress -n erp-prod

# Wait for certificate to be issued (may take 1-2 minutes)
kubectl describe certificate erp-tls-cert -n erp-prod
```

## 🔍 Verify Deployment

### Check All Resources

```bash
# Pods
kubectl get pods -n erp-prod

# Services
kubectl get svc -n erp-prod

# Ingress
kubectl get ingress -n erp-prod

# Certificates
kubectl get certificate -n erp-prod

# Linkerd status
linkerd check --proxy -n erp-prod
```

### Test Endpoints

```bash
# Test API Gateway
curl https://api.yourdomain.com/health/live

# Test Frontend
curl https://erp.yourdomain.com

# Test Grafana
curl https://grafana.yourdomain.com
```

### View Logs

```bash
# All pods
kubectl logs -f -n erp-prod --all-containers=true

# Specific service
kubectl logs -f -n erp-prod deployment/user-management

# Ingress controller logs
kubectl logs -f -n kube-system deployment/traefik
```

## 🔄 CI/CD Setup

### Configure GitHub Secrets

In your GitHub repository, go to Settings > Secrets and variables > Actions, add:

- `SSH_HOST`: Your server IP or domain
- `SSH_USER`: deploy
- `SSH_PRIVATE_KEY`: Private SSH key for deploy user
- `DOCKER_USERNAME`: Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub password or token
- `JWT_SECRET`: Same JWT secret used in cluster
- `MONGODB_PASSWORD`: Same MongoDB password
- `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`: Email credentials

### Setup SSH Key for CI/CD

```bash
# On your server as deploy user
ssh-keygen -t ed25519 -C "github-actions"

# Display public key
cat ~/.ssh/id_ed25519.pub

# Add to authorized_keys
cat ~/.ssh/id_ed25519.pub >> ~/.ssh/authorized_keys

# Display private key (copy this to GitHub secret SSH_PRIVATE_KEY)
cat ~/.ssh/id_ed25519
```

### Authorize Docker Registry

```bash
# Login to Docker Hub
docker login
# Enter credentials

# Verify can pull images
docker pull your-username/erp-user-management:latest
```

## 📊 Setup Monitoring

### Access Grafana

```bash
# Get Grafana admin password
kubectl get secret -n erp-prod grafana-admin-secret -o jsonpath='{.data.password}' | base64 -d

# Access Grafana at https://grafana.yourdomain.com
# Login with admin / <password>
```

### Access Linkerd Dashboard

```bash
# Port forward Linkerd dashboard
kubectl port-forward -n linkerd svc/web 8084:8084

# SSH tunnel from local machine
ssh -L 8084:localhost:8084 deploy@your-server-ip

# Access at http://localhost:8084
```

## 🔧 Maintenance

### Update Application

```bash
# Pull latest code
cd ~/erp
git pull origin main

# Apply changes
kubectl apply -f infrastructure/k8s/base/
kubectl apply -f infrastructure/k8s/production/

# Or use CI/CD pipeline which deploys automatically
```

### Scale Services

```bash
# Scale deployment
kubectl scale deployment user-management --replicas=2 -n erp-prod

# Verify
kubectl get pods -n erp-prod -l app=user-management
```

### Rollback Deployment

```bash
# View rollout history
kubectl rollout history deployment/user-management -n erp-prod

# Rollback to previous version
kubectl rollout undo deployment/user-management -n erp-prod

# Rollback to specific revision
kubectl rollout undo deployment/user-management --to-revision=2 -n erp-prod
```

### Backup MongoDB

```bash
# Create backup directory
mkdir -p ~/backups

# Backup all databases
kubectl exec -n erp-prod mongodb-0 -- mongosh \
  -u admin -p $MONGO_PASSWORD \
  --eval "db.adminCommand({backup: 1})"

# Or export specific database
kubectl exec -n erp-prod mongodb-0 -- mongodump \
  -u admin -p $MONGO_PASSWORD \
  --db erp_users \
  --out /data/backup

# Copy backup to local
kubectl cp erp-prod/mongodb-0:/data/backup ~/backups/$(date +%Y%m%d)
```

### View Resource Usage

```bash
# Node resources
kubectl top nodes

# Pod resources
kubectl top pods -n erp-prod

# Specific pod details
kubectl describe pod -n erp-prod <pod-name>
```

## 🚨 Troubleshooting

### Pods Not Starting

```bash
kubectl describe pod -n erp-prod <pod-name>
kubectl logs -n erp-prod <pod-name>
kubectl get events -n erp-prod --sort-by='.lastTimestamp'
```

### Certificate Issues

```bash
# Check certificate status
kubectl describe certificate erp-tls-cert -n erp-prod

# Check cert-manager logs
kubectl logs -n cert-manager deployment/cert-manager

# Delete and recreate certificate
kubectl delete certificate erp-tls-cert -n erp-prod
kubectl delete secret erp-tls-cert -n erp-prod
kubectl apply -f infrastructure/k8s/production/ingress.yaml
```

### DNS Not Resolving

```bash
# Test DNS from pod
kubectl run -it --rm debug --image=busybox --restart=Never -- nslookup erp.yourdomain.com

# Check external DNS
nslookup erp.yourdomain.com 8.8.8.8
```

### Service Not Reachable

```bash
# Check service endpoints
kubectl get endpoints -n erp-prod

# Test from within cluster
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- \
  curl http://user-management-service:8080/health/live
```

## 📈 Performance Optimization

### Increase Resources

Edit deployment YAML to increase resource limits:

```yaml
resources:
  requests:
    memory: "512Mi"
    cpu: "250m"
  limits:
    memory: "1Gi"
    cpu: "500m"
```

### Enable Horizontal Pod Autoscaling

```bash
kubectl autoscale deployment user-management \
  --cpu-percent=70 \
  --min=1 \
  --max=3 \
  -n erp-prod
```

### Optimize MongoDB

```bash
# Connect to MongoDB
kubectl exec -it -n erp-prod mongodb-0 -- mongosh -u admin -p $MONGO_PASSWORD

# Create indexes
use erp_users
db.users.createIndex({email: 1}, {unique: true})

# Check slow queries
db.setProfilingLevel(1, 100)
db.system.profile.find().limit(10).sort({ts: -1})
```

## 🔒 Security Hardening

### Regular Updates

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Update K3s
curl -sfL https://get.k3s.io | sh -

# Update Linkerd
linkerd upgrade | kubectl apply -f -
```

### Network Policies

Implement Kubernetes NetworkPolicies to restrict pod-to-pod communication.

### Secrets Rotation

Regularly rotate secrets:
```bash
# Generate new JWT secret
NEW_JWT_SECRET=$(openssl rand -base64 32)

# Update secret
kubectl create secret generic jwt-secret \
  --from-literal=secret=$NEW_JWT_SECRET \
  -n erp-prod \
  --dry-run=client -o yaml | kubectl apply -f -

# Restart pods to use new secret
kubectl rollout restart deployment -n erp-prod
```

## 📞 Support

For production issues:
- Check logs: `kubectl logs -n erp-prod <pod-name>`
- Check events: `kubectl get events -n erp-prod`
- Check Linkerd: `linkerd check --proxy -n erp-prod`
- Review monitoring in Grafana
- Contact: support@yourdomain.com
