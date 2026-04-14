#!/usr/bin/env bash
# =============================================================================
# cert-manager/install-azure.sh
# Installs cert-manager on AKS with Kubernetes Gateway API support enabled,
# then creates Let's Encrypt ClusterIssuers using the gatewayHTTPRoute http01
# solver (Traefik / Gateway API).
#
# Run ONCE after aks-provision.sh, before the first aks-deploy.sh run:
#   KUBECONFIG=~/.kube/aks-erp.yaml bash infrastructure/cert-manager/install-azure.sh
#
# cert-manager then manages the TLS certificate referenced in
# infrastructure/k8s/azure/gateway.yaml (secretName: erp-azure-tls-cert).
#
# How TLS issuance works:
#   1. aks-deploy.sh applies the Gateway resource (with cert-manager annotation)
#   2. cert-manager sees the annotation, reads the HTTPS listener hostnames
#   3. cert-manager creates an HTTPRoute on the web (HTTP/80) listener for the
#      ACME http01 challenge
#   4. Let's Encrypt verifies the challenge via Traefik -> HTTPRoute
#   5. cert-manager stores the issued cert in 'erp-azure-tls-cert' secret
#   6. Traefik picks up the secret and serves HTTPS
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../../.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
  echo "❌ Missing .env.deploy"
  exit 1
fi
source "$ENV_FILE"

KUBECONFIG="${KUBECONFIG:-$HOME/.kube/aks-erp.yaml}"
export KUBECONFIG

CERT_MANAGER_VERSION="v1.14.5"
EMAIL="${ACME_EMAIL}"
NAMESPACE="erp-azure"

echo "════════════════════════════════════════════════════════════"
echo " cert-manager installer (AKS + Traefik Gateway API)"
echo " Cluster  : $(kubectl config current-context 2>/dev/null || echo unknown)"
echo " Email    : $EMAIL"
echo " Version  : $CERT_MANAGER_VERSION"
echo "════════════════════════════════════════════════════════════"

echo ""
echo "▶ Installing cert-manager $CERT_MANAGER_VERSION via Helm..."
echo "  (Using Helm so we can enable ExperimentalGatewayAPISupport feature gate)"

helm repo add jetstack https://charts.jetstack.io --force-update
helm repo update

# Install CRDs separately so they are never removed by helm uninstall
echo "  Installing cert-manager CRDs..."
kubectl apply -f "https://github.com/cert-manager/cert-manager/releases/download/${CERT_MANAGER_VERSION}/cert-manager.crds.yaml"
echo "  ✓ CRDs applied"

# AKS runs "admissionsenforcer" which modifies webhook namespaceSelectors and
# causes Helm apply conflicts on re-installs. Delete the stale webhook config
# left by any previous failed install before proceeding.
kubectl delete validatingwebhookconfiguration cert-manager-webhook 2>/dev/null || true

helm upgrade --install cert-manager jetstack/cert-manager \
  --namespace cert-manager --create-namespace \
  --version "$CERT_MANAGER_VERSION" \
  --skip-crds \
  --set "extraArgs={--feature-gates=ExperimentalGatewayAPISupport=true}" \
  --set startupapicheck.enabled=false \
  --wait --timeout 10m

echo "  ✓ cert-manager ready"
kubectl get pods -n cert-manager

echo ""
echo "▶ Waiting for cert-manager CRDs to be available in the API..."
kubectl wait --for=condition=established crd/clusterissuers.cert-manager.io --timeout=60s
echo "  ✓ CRDs ready"

echo ""
echo "▶ Creating Let's Encrypt ClusterIssuers (http01 via Traefik Gateway API)..."
echo "  The gatewayHTTPRoute solver creates HTTPRoutes on the web (port 80)"
echo "  listener of the erp-gateway Gateway for ACME challenge validation."

kubectl apply -f - <<EOF
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: ${EMAIL}
    privateKeySecretRef:
      name: letsencrypt-prod-key
    solvers:
      - http01:
          gatewayHTTPRoute:
            parentRefs:
              - name: erp-gateway
                namespace: ${NAMESPACE}
                sectionName: web
---
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-staging
spec:
  acme:
    server: https://acme-staging-v02.api.letsencrypt.org/directory
    email: ${EMAIL}
    privateKeySecretRef:
      name: letsencrypt-staging-key
    solvers:
      - http01:
          gatewayHTTPRoute:
            parentRefs:
              - name: erp-gateway
                namespace: ${NAMESPACE}
                sectionName: web
EOF

echo ""
echo "  ✓ ClusterIssuers created"
echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  cert-manager ready for AKS + Traefik Gateway API"
echo ""
echo "  After running aks-deploy.sh, watch certificate issuance:"
echo "    KUBECONFIG=~/.kube/aks-erp.yaml \\"
echo "      kubectl get certificate,certificaterequest -n erp-azure -w"
echo "════════════════════════════════════════════════════════════"
