#!/usr/bin/env bash
# =============================================================================
# aks-stop.sh
# Stops the AKS cluster and PostgreSQL server to eliminate compute costs.
# All Kubernetes state is PRESERVED — no re-deploy needed when starting again.
#
# Residual cost while stopped: ~€11/month (public IP + storage + ACR).
# Note: Azure auto-restarts stopped PostgreSQL after 7 days.
#
# Usage (from repo root):
#   bash scripts/aks-stop.sh
#
# Prerequisites:
#   - Azure CLI logged in: az login
#   - .env.deploy filled in with Azure variables
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
  echo "❌ Missing .env.deploy — copy .env.deploy.example and fill in your values"
  exit 1
fi
source "$ENV_FILE"

echo "════════════════════════════════════════════════════════════"
echo " Stopping Azure ERP infrastructure"
echo " Resource Group : $AZ_RESOURCE_GROUP"
echo " AKS            : $AZ_AKS_NAME"
echo " PostgreSQL     : $AZ_PG_SERVER_NAME"
echo "════════════════════════════════════════════════════════════"
echo ""

echo "▶ Stopping AKS cluster..."
az aks stop --resource-group "$AZ_RESOURCE_GROUP" --name "$AZ_AKS_NAME"
echo "  ✓ AKS cluster stopped"

echo ""
echo "▶ Stopping PostgreSQL..."
az postgres flexible-server stop --resource-group "$AZ_RESOURCE_GROUP" --name "$AZ_PG_SERVER_NAME"
echo "  ✓ PostgreSQL stopped"

echo ""
echo "✅ Stopped. Residual cost: ~€11/month (IP + storage + ACR)."
echo "   To resume: bash scripts/aks-start.sh"
