#!/usr/bin/env bash
# =============================================================================
# aks-delete.sh
# ⚠️  PERMANENTLY deletes the entire Azure resource group and ALL resources
# within it: AKS cluster, ACR, Key Vault, PostgreSQL, VNet, public IPs, etc.
#
# This action is IRREVERSIBLE. All data will be lost.
#
# Usage (from repo root):
#   bash scripts/aks-delete.sh
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
echo " ⚠️  PERMANENT DELETION WARNING"
echo "════════════════════════════════════════════════════════════"
echo ""
echo "  This will PERMANENTLY DELETE the following resource group"
echo "  and ALL resources contained within it:"
echo ""
echo "    Resource Group : $AZ_RESOURCE_GROUP"
echo "    Location       : $AZ_LOCATION"
echo ""
echo "  This includes:"
echo "    • AKS cluster      ($AZ_AKS_NAME)"
echo "    • Container Registry ($AZ_ACR_NAME) — all images will be lost"
echo "    • Key Vault        ($AZ_KEYVAULT_NAME) — all secrets will be lost"
echo "    • PostgreSQL       ($AZ_PG_SERVER_NAME) — all data will be lost"
echo "    • Virtual Network  ($AZ_VNET_NAME)"
echo "    • All public IPs, storage, and other resources in the group"
echo ""
echo "  ❌ THIS ACTION CANNOT BE UNDONE."
echo ""

read -r -p "Type DELETE (all caps) to confirm: " CONFIRMATION

if [ "$CONFIRMATION" != "DELETE" ]; then
  echo ""
  echo "Aborted. Nothing was deleted."
  exit 1
fi

echo ""
echo "▶ Deleting resource group '$AZ_RESOURCE_GROUP'..."
echo "  (This may take several minutes)"
az group delete --name "$AZ_RESOURCE_GROUP" --yes

echo ""
echo "✅ Resource group '$AZ_RESOURCE_GROUP' and all resources permanently deleted."
echo "   Run scripts/aks-provision.sh to provision fresh infrastructure."
