#!/usr/bin/env bash
# =============================================================================
# aks-deploy.sh
# Deploys the ERP system to Azure AKS.
#
# Usage (from repo root):
#   bash scripts/aks-deploy.sh
#   TAG=v1.2.3 bash scripts/aks-deploy.sh
#
# Prerequisites:
#   - .env.deploy filled with Azure variables (including those printed by
#     aks-provision.sh: AZURE_TENANT_ID, AZURE_KV_IDENTITY_CLIENT_ID, AZ_PG_HOST)
#   - Images already pushed: bash scripts/aks-build-push.sh
#   - KUBECONFIG at ~/.kube/aks-erp.yaml (created by aks-provision.sh)
#
# Secret strategy:
#   - grafana-admin-secret : synced from Key Vault via CSI driver (secret-sync pod)
#   - postgres-secret      : created imperatively AFTER kustomize apply so it
#                            wins over the base's local connection strings
#   - rabbitmq-secret      : from base (self-hosted RabbitMQ, guest/guest)
# =============================================================================
set -euo pipefail

# ── Prefer Rancher Desktop kubectl if available (avoids stale /usr/local/bin) ─
if [ -d "$HOME/.rd/bin" ]; then
  export PATH="$HOME/.rd/bin:$PATH"
fi

# ── Require kubectl ≥ 1.27 (older kustomize has UTF-8 YAML bugs) ─────────────
KUBECTL_MAJOR=$(kubectl version --client -o json 2>/dev/null | python3 -c "import sys,json;print(json.load(sys.stdin)['clientVersion']['major'])" 2>/dev/null || echo 0)
KUBECTL_MINOR=$(kubectl version --client -o json 2>/dev/null | python3 -c "import sys,json;print(json.load(sys.stdin)['clientVersion']['minor'])" 2>/dev/null || echo 0)
if [ "$KUBECTL_MAJOR" -lt 1 ] || { [ "$KUBECTL_MAJOR" -eq 1 ] && [ "$KUBECTL_MINOR" -lt 27 ]; }; then
  echo "❌ kubectl $(kubectl version --client --short 2>/dev/null || echo 'unknown') is too old (need ≥ 1.27)."
  echo "   Found: $(which kubectl)"
  echo "   Install a recent kubectl or ensure Rancher Desktop's ~/.rd/bin is first in PATH."
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
  echo "❌ Missing .env.deploy — copy .env.deploy.example and fill in your values"
  exit 1
fi
_TAG_OVERRIDE="${TAG:-}"
source "$ENV_FILE"
[ -n "$_TAG_OVERRIDE" ] && TAG="$_TAG_OVERRIDE"

# ── Required variables ──────────────────────────────────────────────────────
: "${AZ_ACR_NAME:?AZ_ACR_NAME not set in .env.deploy}"
: "${AZ_RESOURCE_GROUP:?AZ_RESOURCE_GROUP not set in .env.deploy}"
: "${AZ_KEYVAULT_NAME:?AZ_KEYVAULT_NAME not set in .env.deploy}"
: "${AZURE_KV_IDENTITY_CLIENT_ID:?Run aks-provision.sh and set AZURE_KV_IDENTITY_CLIENT_ID in .env.deploy}"
: "${AZURE_TENANT_ID:?Run aks-provision.sh and set AZURE_TENANT_ID in .env.deploy}"
: "${AZ_PG_HOST:?Run aks-provision.sh and set AZ_PG_HOST in .env.deploy}"
: "${AZ_PG_ADMIN_USER:?AZ_PG_ADMIN_USER not set in .env.deploy}"
: "${AZ_PG_ADMIN_PASSWORD:?AZ_PG_ADMIN_PASSWORD not set in .env.deploy}"
: "${AZURE_DOMAIN:?AZURE_DOMAIN not set in .env.deploy}"

REGISTRY="${AZ_ACR_NAME}.azurecr.io"
TAG="${TAG:-latest}"
KUBECONFIG="${KUBECONFIG:-$HOME/.kube/aks-erp.yaml}"
NAMESPACE="erp-azure"
KV_NAME="${AZ_KEYVAULT_NAME}"
KV_IDENTITY="${AZURE_KV_IDENTITY_CLIENT_ID}"
TENANT_ID="${AZURE_TENANT_ID}"
DOMAIN="${AZURE_DOMAIN}"

export KUBECONFIG

echo "════════════════════════════════════════════════════════════"
echo " ERP Deploy → AKS"
echo " Registry  : $REGISTRY"
echo " Tag       : $TAG"
echo " Namespace : $NAMESPACE"
echo " Domain    : $DOMAIN"
echo "════════════════════════════════════════════════════════════"

echo ""
echo "▶ Verifying cluster connection..."
kubectl cluster-info --context "$(kubectl config current-context)"

echo ""
echo "▶ Ensuring namespace '$NAMESPACE' exists..."
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -

# ── Build temporary overlay with all runtime values ───────────────────────────
# The azure/kustomization.yaml has placeholder values for domain, Key Vault
# identity, and image tags. We overlay them here at deploy time so nothing
# sensitive or environment-specific lives in source-controlled YAML.
TMP_OVERLAY="infrastructure/k8s/_deploy-tmp-azure"
mkdir -p "$TMP_OVERLAY"
trap 'rm -rf "$TMP_OVERLAY"' EXIT

cat > "$TMP_OVERLAY/kustomization.yaml" <<EOF
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
resources:
  - ../azure
# -- Inject concrete ACR image tags ------------------------------------------
images:
  - name: erp/gateway
    newName: ${REGISTRY}/erp-gateway
    newTag: "${TAG}"
  - name: erp/user-management
    newName: ${REGISTRY}/erp-user-management
    newTag: "${TAG}"
  - name: erp/inventory
    newName: ${REGISTRY}/erp-inventory
    newTag: "${TAG}"
  - name: erp/sales
    newName: ${REGISTRY}/erp-sales
    newTag: "${TAG}"
  - name: erp/financial
    newName: ${REGISTRY}/erp-financial
    newTag: "${TAG}"
  - name: erp/dashboard
    newName: ${REGISTRY}/erp-dashboard
    newTag: "${TAG}"
  - name: erp/orchestration
    newName: ${REGISTRY}/erp-orchestration
    newTag: "${TAG}"
  - name: erp/frontend
    newName: ${REGISTRY}/erp-frontend
    newTag: "${TAG}"
patches:
  # -- Inject Key Vault identity into SecretProviderClass ----------------------
  - target:
      kind: SecretProviderClass
      name: azure-keyvault-erp
    patch: |-
      - op: replace
        path: /spec/parameters/clientID
        value: "${KV_IDENTITY}"
      - op: replace
        path: /spec/parameters/keyvaultName
        value: "${KV_NAME}"
      - op: replace
        path: /spec/parameters/tenantId
        value: "${TENANT_ID}"
  # -- Inject domain into Gateway listener hostnames --------------------------
  # Listeners 0 (web/HTTP) has no hostname — only the 3 HTTPS listeners need
  # the concrete domain value. Indices match the order in gateway.yaml:
  #   [0] web (no hostname), [1] websecure, [2] websecure-www, [3] websecure-monitoring
  - target:
      kind: Gateway
      name: erp-gateway
    patch: |-
      - op: replace
        path: /spec/listeners/1/hostname
        value: "${DOMAIN}"
      - op: replace
        path: /spec/listeners/1/tls/certificateRefs/0/name
        value: "erp-azure-tls-cert"
      - op: replace
        path: /spec/listeners/2/hostname
        value: "www.${DOMAIN}"
      - op: replace
        path: /spec/listeners/3/hostname
        value: "monitoring.${DOMAIN}"
  # -- Inject Grafana domain (add env vars for GF_SERVER_DOMAIN) ---------------
  - target:
      kind: Deployment
      name: grafana
    patch: |-
      - op: add
        path: /spec/template/spec/containers/0/env/-
        value:
          name: GF_SERVER_DOMAIN
          value: "monitoring.${DOMAIN}"
      - op: add
        path: /spec/template/spec/containers/0/env/-
        value:
          name: GF_SERVER_ROOT_URL
          value: "https://monitoring.${DOMAIN}"
EOF

echo "  ✓ Temporary overlay prepared"

# ── Phase 1: Apply full overlay ───────────────────────────────────────────────
echo ""
echo "▶ Phase 1 — Applying manifests..."
kubectl apply -k "$TMP_OVERLAY"
echo "  ✓ Manifests applied"

# ── Phase 2: Wait for secret-sync (CSI driver syncs grafana-admin-secret) ─────
echo ""
echo "▶ Phase 2 — Waiting for secret-sync pod (syncs Key Vault → K8s Secrets)..."
# Restart rollout first — if a previous deploy timed out, the deployment's
# progress deadline is already marked as exceeded and rollout status would
# fail immediately without actually waiting.
kubectl rollout restart deployment/secret-sync -n "$NAMESPACE"
kubectl rollout status deployment/secret-sync -n "$NAMESPACE" --timeout=120s
echo "  ✓ secret-sync running — grafana-admin-secret synced from Key Vault"

# ── Phase 3: Create postgres-secret with Azure DB connection strings ──────────
# The base postgres.yaml defines postgres-secret with local (pod) connection
# strings. We override it here with Azure DB connection strings pulled from
# Key Vault. Running AFTER kustomize apply means this version wins.
echo ""
echo "▶ Phase 3 — Creating postgres-secret from Azure Key Vault..."

PG_SSL="Ssl Mode=Require;Trust Server Certificate=true"
PG_BASE="Host=${AZ_PG_HOST};Port=5432;Username=${AZ_PG_ADMIN_USER};Password=${AZ_PG_ADMIN_PASSWORD};${PG_SSL}"

kubectl create secret generic postgres-secret \
  --namespace "$NAMESPACE" \
  --from-literal=connection-string-users="${PG_BASE};Database=erp_users" \
  --from-literal=connection-string-inventory="${PG_BASE};Database=erp_inventory" \
  --from-literal=connection-string-sales="${PG_BASE};Database=erp_sales" \
  --from-literal=connection-string-financial="${PG_BASE};Database=erp_financial" \
  --from-literal=connection-string-dashboard="${PG_BASE};Database=erp_dashboard" \
  --from-literal=connection-string-orchestration="${PG_BASE};Database=erp_orchestration" \
  --dry-run=client -o yaml | kubectl apply -f -
echo "  ✓ postgres-secret updated with Azure DB connection strings"

# ── Phase 4: Rollout restart to pick up correct secrets ───────────────────────
# ERP pods that started before postgres-secret was updated need to be restarted
# so they read the correct connection strings from their env vars on startup.
echo ""
echo "▶ Phase 4 — Restarting ERP services to pick up updated secrets..."
ERP_DEPLOYMENTS=(gateway user-management inventory sales financial dashboard orchestration frontend)
for dep in "${ERP_DEPLOYMENTS[@]}"; do
  kubectl rollout restart deployment/"$dep" -n "$NAMESPACE" 2>/dev/null || true
done
echo "  ✓ Rollout restarts triggered"

# ── Phase 5: Wait for rollouts ────────────────────────────────────────────────
echo ""
echo "▶ Phase 5 — Waiting for ERP service rollouts..."
for dep in "${ERP_DEPLOYMENTS[@]}"; do
  echo -n "  $dep ... "
  kubectl rollout status deployment/"$dep" -n "$NAMESPACE" --timeout=300s
done

# ── Status ────────────────────────────────────────────────────────────────────
echo ""
echo "▶ Pod status:"
kubectl get pods -n "$NAMESPACE" -o wide

echo ""
echo "▶ Gateway:"
kubectl get gateway -n "$NAMESPACE"
kubectl get httproute -n "$NAMESPACE"

echo ""
echo "▶ Services:"
kubectl get services -n "$NAMESPACE"

TRAEFIK_IP=$(kubectl get service traefik -n traefik \
  -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "unknown")

echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  Deployment complete!"
echo ""
echo "  App URL      : https://${DOMAIN}"
echo "  Monitoring   : https://monitoring.${DOMAIN}"
echo "  Traefik LB IP: ${TRAEFIK_IP}"
echo ""
echo "  DNS: ensure A records point ${DOMAIN} → ${TRAEFIK_IP}"
echo "════════════════════════════════════════════════════════════"
