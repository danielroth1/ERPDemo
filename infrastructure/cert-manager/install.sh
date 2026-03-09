#!/usr/bin/env bash
# =============================================================================
# cert-manager/install.sh
# Installs cert-manager on the remote k3s cluster and creates Let's Encrypt
# ClusterIssuers (prod + staging).
#
# Run from repo root:
#   KUBECONFIG=~/.kube/k3s-erp.yaml bash infrastructure/cert-manager/install.sh
#
# k3s uses Traefik as its built-in ingress controller (NOT nginx).
# The ClusterIssuers below use http01 with ingressClass: traefik.
# =============================================================================
set -euo pipefail

# ── Load local deploy config ─────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../../.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
    echo "❌ Missing .env.deploy — copy .env.deploy.example and fill in your values"
    exit 1
fi
# shellcheck source=../../.env.deploy
source "$ENV_FILE"

KUBECONFIG="${KUBECONFIG:-$HOME/.kube/k3s-erp.yaml}"
export KUBECONFIG

CERT_MANAGER_VERSION="v1.14.5"

echo "════════════════════════════════════════════════════════════"
echo " cert-manager installer"
echo " Cluster  : $(kubectl config current-context 2>/dev/null || echo unknown)"
echo " Domain   : $DOMAIN"
echo " Email    : $ACME_EMAIL"
echo " Version  : $CERT_MANAGER_VERSION"
echo "════════════════════════════════════════════════════════════"

# ── 1. Check for existing cert ──────────────────────────────────────────────
echo ""
echo "▶ Checking for existing certificate..."
if kubectl get secret erp-tls-cert -n erp-prod &>/dev/null; then
  echo "  ✓ Secret 'erp-tls-cert' already exists in erp-prod."
  echo "  To force renewal: kubectl delete secret erp-tls-cert -n erp-prod"
else
  echo "  No existing certificate found — will create one."
fi

# ── 2. Install cert-manager ──────────────────────────────────────────────────
echo ""
echo "▶ Installing cert-manager $CERT_MANAGER_VERSION..."
kubectl apply -f "https://github.com/cert-manager/cert-manager/releases/download/${CERT_MANAGER_VERSION}/cert-manager.yaml"

echo "  Waiting for cert-manager webhook to be ready (up to 5 min)..."
kubectl wait --for=condition=Available --timeout=300s \
  deployment/cert-manager-webhook -n cert-manager
echo "  ✓ cert-manager ready"
kubectl get pods -n cert-manager

# ── 3. Create ClusterIssuers ─────────────────────────────────────────────────
echo ""
echo "▶ Creating Let's Encrypt ClusterIssuers (traefik http01 solver)..."

kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: ${ACME_EMAIL}
    privateKeySecretRef:
      name: letsencrypt-prod-key
    solvers:
      - http01:
          ingress:
            ingressClassName: traefik
---
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-staging
spec:
  acme:
    server: https://acme-staging-v02.api.letsencrypt.org/directory
    email: ${ACME_EMAIL}
    privateKeySecretRef:
      name: letsencrypt-staging-key
    solvers:
      - http01:
          ingress:
            ingressClassName: traefik
EOF

echo "  ✓ ClusterIssuers created"
kubectl get clusterissuer

echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  cert-manager installed!"
echo ""
echo " Next: run the deploy task to apply the TLS ingress."
echo " cert-manager will automatically request a certificate for $DOMAIN."
echo ""
echo " Monitor issuance:"
echo "   kubectl describe certificate erp-tls-cert -n erp-prod"
echo "   kubectl describe certificaterequest -n erp-prod"
echo "   kubectl describe order -n erp-prod"
echo "════════════════════════════════════════════════════════════"
