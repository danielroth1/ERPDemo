#!/bin/bash
set -e

echo "🔧 Installing cert-manager..."

# Check if cert-manager namespace already exists
if kubectl get namespace cert-manager >/dev/null 2>&1; then
    echo "✓ cert-manager namespace already exists"
else
    echo "Creating cert-manager namespace..."
    kubectl create namespace cert-manager
fi

# Install cert-manager CRDs
echo "Installing cert-manager CRDs..."
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.crds.yaml

# Install cert-manager
echo "Installing cert-manager components..."
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Wait for cert-manager to be ready
echo "Waiting for cert-manager webhook to be ready..."
kubectl wait --for=condition=Available --timeout=300s deployment/cert-manager-webhook -n cert-manager

# Verify installation
echo "Verifying cert-manager installation..."
kubectl get pods -n cert-manager

# Create ClusterIssuer for Let's Encrypt
echo "Creating Let's Encrypt ClusterIssuer..."
kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: admin@yourdomain.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF

# Create staging issuer for testing
echo "Creating Let's Encrypt Staging ClusterIssuer..."
kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-staging
spec:
  acme:
    server: https://acme-staging-v02.api.letsencrypt.org/directory
    email: admin@yourdomain.com
    privateKeySecretRef:
      name: letsencrypt-staging
    solvers:
    - http01:
        ingress:
          class: nginx
EOF

# Verify ClusterIssuers
echo "Verifying ClusterIssuers..."
kubectl get clusterissuer

echo "✅ cert-manager installation completed successfully!"
echo ""
echo "📝 Next steps:"
echo "1. Update the email address in ClusterIssuers: kubectl edit clusterissuer letsencrypt-prod"
echo "2. Ensure your DNS records point to your server's IP address"
echo "3. Make sure ports 80 and 443 are open in your firewall"
echo "4. Add TLS annotations to your Ingress resources:"
echo "   cert-manager.io/cluster-issuer: \"letsencrypt-prod\""
