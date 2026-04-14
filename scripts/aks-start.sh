#!/usr/bin/env bash
# =============================================================================
# aks-start.sh
# Resumes the AKS cluster and PostgreSQL server.
# No re-deploy needed — Kubernetes state is fully preserved from before stop.
#
# Wait ~3-5 min after start for nodes to come up and pods to reschedule.
#
# Usage (from repo root):
#   bash scripts/aks-start.sh
#
# Prerequisites:
#   - Azure CLI logged in: az login
#   - .env.deploy filled in with Azure variables
# =============================================================================
set -euo pipefail

# ── Prefer Rancher Desktop kubectl if available (avoids stale /usr/local/bin) ─
if [ -d "$HOME/.rd/bin" ]; then
  export PATH="$HOME/.rd/bin:$PATH"
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
  echo "❌ Missing .env.deploy — copy .env.deploy.example and fill in your values"
  exit 1
fi
source "$ENV_FILE"

echo "════════════════════════════════════════════════════════════"
echo " Starting Azure ERP infrastructure"
echo " Resource Group : $AZ_RESOURCE_GROUP"
echo " AKS            : $AZ_AKS_NAME"
echo " PostgreSQL     : $AZ_PG_SERVER_NAME"
echo "════════════════════════════════════════════════════════════"
echo ""

echo "▶ Starting PostgreSQL..."
az postgres flexible-server start --resource-group "$AZ_RESOURCE_GROUP" --name "$AZ_PG_SERVER_NAME"
echo "  ✓ PostgreSQL started"

echo ""
echo "▶ Starting AKS cluster (takes ~3-5 min)..."
az aks start --resource-group "$AZ_RESOURCE_GROUP" --name "$AZ_AKS_NAME"
echo "  ✓ AKS cluster started"

echo ""
echo "✅ Started. Pods will reschedule automatically."
echo "   Check status: KUBECONFIG=~/.kube/aks-erp.yaml kubectl get pods -n erp-azure"
