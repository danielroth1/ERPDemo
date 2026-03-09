#!/usr/bin/env bash
# =============================================================================
# k3s-deploy.sh
# Deploys the ERP system to the remote k3s server at ${K3S_SERVER}
#
# Usage (from repo root):
#   export REGISTRY=ghcr.io/YOURUSER    # must match what you used in build-push
#   export TAG=latest                   # or a git SHA: $(git rev-parse --short HEAD)
#   ./scripts/k3s-deploy.sh
#
# Prerequisites:
#   - kubectl installed locally (https://kubernetes.io/docs/tasks/tools/)
#   - Kubeconfig at ~/.kube/k3s-erp.yaml  (created by k3s-setup-server.sh)
#   - Images already pushed (k3s-build-push.sh)
#   - NO standalone kustomize needed: kubectl apply -k uses kustomize built-in
# =============================================================================
# NOTE: If you don't have kubectl locally, you can SSH to the server and run
# this script there (k3s ships kubectl at /usr/local/bin/kubectl on the server)
set -euo pipefail

# ── Load local deploy config ─────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
    echo "❌ Missing .env.deploy — copy .env.deploy.example and fill in your values"
    exit 1
fi
# Save runtime overrides before sourcing (so REGISTRY=x ./script.sh still works)
_REGISTRY_OVERRIDE="${REGISTRY:-}"
_TAG_OVERRIDE="${TAG:-}"
_GHCR_TOKEN_OVERRIDE="${GHCR_TOKEN:-}"
# shellcheck source=../.env.deploy
source "$ENV_FILE"
[ -n "$_REGISTRY_OVERRIDE" ] && REGISTRY="$_REGISTRY_OVERRIDE"
[ -n "$_TAG_OVERRIDE" ] && TAG="$_TAG_OVERRIDE"
[ -n "$_GHCR_TOKEN_OVERRIDE" ] && GHCR_TOKEN="$_GHCR_TOKEN_OVERRIDE"

# ── Configuration ─────────────────────────────────────────────────────────────
REGISTRY="${REGISTRY}"
TAG="${TAG:-latest}"
KUBECONFIG="${KUBECONFIG:-$HOME/.kube/k3s-erp.yaml}"
OVERLAY_DIR="infrastructure/k8s/production"
NAMESPACE="erp-prod"

export KUBECONFIG

echo "════════════════════════════════════════════════════════════"
echo " ERP Deploy → k3s at ${K3S_SERVER#*@}"
echo " Registry  : $REGISTRY"
echo " Tag       : $TAG"
echo " Namespace : $NAMESPACE"
echo " Kubeconfig: $KUBECONFIG"
echo "════════════════════════════════════════════════════════════"

# Verify cluster connectivity
echo ""
echo "▶ Verifying cluster connection..."
kubectl cluster-info

# ── Patch image names/tags inline (no standalone kustomize CLI needed) ────────
# We generate a temporary kustomization overlay that inherits the production
# one and just overrides the image tags. kubectl apply -k uses built-in kustomize.
echo ""
# ── Validate GHCR token ──────────────────────────────────────────────────────
if [ -z "${GHCR_TOKEN:-}" ]; then
  echo ""
  echo "ERROR: GHCR_TOKEN is not set."
  echo "  Create a GitHub Personal Access Token with 'read:packages' scope at:"
  echo "  https://github.com/settings/tokens"
  echo "  Then export it: export GHCR_TOKEN=ghp_yourTokenHere"
  exit 1
fi

# ── Ensure namespace exists before creating secrets ───────────────────────────
echo ""
echo "▶ Ensuring namespace '$NAMESPACE' exists..."
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -

# ── Create / refresh GHCR image pull secret ───────────────────────────────────
echo ""
echo "▶ Creating/updating GHCR pull secret 'ghcr-secret' in namespace '$NAMESPACE'..."
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username="$YOURUSER" \
  --docker-password="$GHCR_TOKEN" \
  --namespace="$NAMESPACE" \
  --dry-run=client -o yaml | kubectl apply -f -
echo "  ✓ Pull secret ready"

echo ""
echo "▶ Preparing manifests (registry: $REGISTRY, tag: $TAG)..."

TMP_OVERLAY="infrastructure/k8s/_deploy-tmp"
mkdir -p "$TMP_OVERLAY"
trap 'rm -rf "$TMP_OVERLAY"' EXIT

cat > "$TMP_OVERLAY/kustomization.yaml" <<EOF
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../production
images:
  - name: erp/gateway
    newName: $REGISTRY/erp-gateway
    newTag: "$TAG"
  - name: erp/user-management
    newName: $REGISTRY/erp-user-management
    newTag: "$TAG"
  - name: erp/inventory
    newName: $REGISTRY/erp-inventory
    newTag: "$TAG"
  - name: erp/sales
    newName: $REGISTRY/erp-sales
    newTag: "$TAG"
  - name: erp/financial
    newName: $REGISTRY/erp-financial
    newTag: "$TAG"
  - name: erp/dashboard
    newName: $REGISTRY/erp-dashboard
    newTag: "$TAG"
  - name: erp/frontend
    newName: $REGISTRY/erp-frontend
    newTag: "$TAG"
EOF

echo "  ✓ Temporary overlay prepared"

# ── Apply manifests ───────────────────────────────────────────────────────────
echo ""
echo "▶ Applying production manifests..."
kubectl apply -k "$TMP_OVERLAY"

# ── Wait for rollout ──────────────────────────────────────────────────────────
echo ""
echo "▶ Waiting for deployments to be ready..."

DEPLOYMENTS=(
  gateway
  user-management
  inventory
  sales
  financial
  dashboard
  frontend
)

for dep in "${DEPLOYMENTS[@]}"; do
  echo -n "  $dep ... "
  kubectl rollout status deployment/"$dep" -n "$NAMESPACE" --timeout=180s
done

# ── Show status ───────────────────────────────────────────────────────────────
echo ""
echo "▶ Pod status:"
kubectl get pods -n "$NAMESPACE" -o wide

echo ""
echo "▶ Ingress:"
kubectl get ingress -n "$NAMESPACE"

echo ""
echo "▶ Services:"
kubectl get services -n "$NAMESPACE"

# ── Print access URL ──────────────────────────────────────────────────────────
# Traefik is the ingress controller in k3s – it gets the external IP from the
# LoadBalancer service k3s creates for it in kube-system
INGRESS_IP=$(kubectl get service -n kube-system traefik \
  -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "${K3S_SERVER#*@}")

echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  Deployment complete!"
echo ""
echo "  App URL  : http://${INGRESS_IP}"
echo "  (Port 80 is handled by Traefik, k3s built-in ingress)"
echo ""
echo "  If you have a domain, point an A record to ${K3S_SERVER#*@}"
echo "  and update infrastructure/k8s/production/ingress.yaml"
echo "  to add the 'host:' field, then re-run this script."
echo "════════════════════════════════════════════════════════════"
