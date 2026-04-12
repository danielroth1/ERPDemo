#!/usr/bin/env bash
# =============================================================================
# aks-build-push.sh
# Builds all ERP Docker images for linux/amd64 and pushes them to Azure
# Container Registry (ACR).
#
# Usage (from repo root):
#   bash scripts/aks-build-push.sh
#   TAG=v1.2.3 bash scripts/aks-build-push.sh
#
# Prerequisites:
#   - Azure CLI logged in: az login
#   - Docker Desktop / OrbStack with buildx enabled
#   - .env.deploy with AZ_ACR_NAME set
#   - AKS provisioned: bash scripts/aks-provision.sh
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
  echo "❌ Missing .env.deploy — copy .env.deploy.example and fill in your values"
  exit 1
fi
_TAG_OVERRIDE="${TAG:-}"
source "$ENV_FILE"
[ -n "$_TAG_OVERRIDE" ] && TAG="$_TAG_OVERRIDE"

ACR="${AZ_ACR_NAME}"
REGISTRY="${ACR}.azurecr.io"
TAG="${TAG:-latest}"
PLATFORM="linux/amd64"

# Services: <image-name>:<context-dir>:<dockerfile>
declare -a SERVICES=(
  "erp-gateway:services:services/gateway/Dockerfile"
  "erp-user-management:services:services/user-management/Dockerfile"
  "erp-inventory:services:services/inventory/Dockerfile"
  "erp-sales:services:services/sales/Dockerfile"
  "erp-financial:services:services/financial/Dockerfile"
  "erp-dashboard:services:services/dashboard/Dockerfile"
  "erp-orchestration:services:services/orchestration/Dockerfile"
)

echo "════════════════════════════════════════════════════════════"
echo " ERP Build & Push → ACR"
echo " Registry : $REGISTRY"
echo " Tag      : $TAG"
echo " Platform : $PLATFORM"
echo "════════════════════════════════════════════════════════════"

echo ""
echo "▶ Logging in to ACR..."
az acr login --name "$ACR"
echo "  ✓ Logged in"

if ! docker buildx inspect erp-builder &>/dev/null; then
  echo "▶ Creating buildx builder 'erp-builder'..."
  docker buildx create --name erp-builder --driver docker-container --bootstrap
fi
docker buildx use erp-builder

for entry in "${SERVICES[@]}"; do
  IFS=':' read -r name context dockerfile <<< "$entry"
  FULL_IMAGE="$REGISTRY/$name:$TAG"
  echo ""
  echo "▶ Building $FULL_IMAGE"
  echo "  context:    $context"
  echo "  dockerfile: $dockerfile"
  docker buildx build \
    --platform "$PLATFORM" \
    --file "$dockerfile" \
    --tag "$FULL_IMAGE" \
    --push \
    "$context"
  echo "  ✓ Pushed $FULL_IMAGE"
done

FRONTEND_IMAGE="$REGISTRY/erp-frontend:$TAG"
echo ""
echo "▶ Building $FRONTEND_IMAGE"
docker buildx build \
  --platform "$PLATFORM" \
  --file "frontend/Dockerfile" \
  --tag "$FRONTEND_IMAGE" \
  --push \
  frontend/
echo "  ✓ Pushed $FRONTEND_IMAGE"

echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  All images pushed to $REGISTRY"
echo ""
echo " Next step:  bash scripts/aks-deploy.sh"
echo "════════════════════════════════════════════════════════════"
