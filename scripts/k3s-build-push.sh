#!/usr/bin/env bash
# =============================================================================
# k3s-build-push.sh
# Builds all ERP Docker images for linux/amd64 (the k3s server architecture)
# and pushes them to your container registry.
#
# Usage (from repo root):
#   export REGISTRY=ghcr.io/YOURUSER    # or docker.io/YOURUSER
#   export TAG=latest                   # or a git SHA: $(git rev-parse --short HEAD)
#   ./scripts/k3s-build-push.sh
#
# Requirements:
#   - Docker Desktop / OrbStack with buildx enabled (already present on Mac)
#   - Logged in to your registry: docker login ghcr.io  (or docker login)
#
# Apple Silicon note:
#   Your Mac is ARM64. The k3s server is x86-64 (AMD64). buildx handles the
#   cross-compilation transparently – no action needed on your part.
# =============================================================================
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
# shellcheck source=../.env.deploy
source "$ENV_FILE"
[ -n "$_REGISTRY_OVERRIDE" ] && REGISTRY="$_REGISTRY_OVERRIDE"
[ -n "$_TAG_OVERRIDE" ] && TAG="$_TAG_OVERRIDE"

# ── Configuration ─────────────────────────────────────────────────────────────
REGISTRY="${REGISTRY}"   # set via .env.deploy; override with: REGISTRY=x ./k3s-build-push.sh
TAG="${TAG:-latest}"
PLATFORM="linux/amd64"

# Build arg for frontend (relative URL works in k8s; nginx inside the container
# proxies /api/* to gateway-service internally, so no external URL is needed)
VITE_API_URL=""

# Services to build: <image-name>:<context-dir>:<dockerfile>
declare -a SERVICES=(
  "erp-gateway:services/gateway:services/gateway/Dockerfile"
  "erp-user-management:services/user-management:services/user-management/Dockerfile"
  "erp-inventory:services/inventory:services/inventory/Dockerfile"
  "erp-sales:services/sales:services/sales/Dockerfile"
  "erp-financial:services/financial:services/financial/Dockerfile"
  "erp-dashboard:services/dashboard:services/dashboard/Dockerfile"
  "erp-orchestration:services/orchestration:services/orchestration/Dockerfile"
)

echo "════════════════════════════════════════════════════════════"
echo " ERP Build & Push"
echo " Registry : $REGISTRY"
echo " Tag      : $TAG"
echo " Platform : $PLATFORM"
echo "════════════════════════════════════════════════════════════"

# ── Ensure buildx builder exists ──────────────────────────────────────────────
if ! docker buildx inspect erp-builder &>/dev/null; then
  echo "▶ Creating buildx builder 'erp-builder'..."
  docker buildx create --name erp-builder --driver docker-container --bootstrap
fi
docker buildx use erp-builder

# ── Build and push backend services ──────────────────────────────────────────
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

# ── Build and push frontend ───────────────────────────────────────────────────
FRONTEND_IMAGE="$REGISTRY/erp-frontend:$TAG"
echo ""
echo "▶ Building $FRONTEND_IMAGE"
docker buildx build \
  --platform "$PLATFORM" \
  --file "frontend/Dockerfile" \
  --tag "$FRONTEND_IMAGE" \
  --build-arg "VITE_API_GATEWAY_URL=$VITE_API_URL" \
  --push \
  frontend/
echo "  ✓ Pushed $FRONTEND_IMAGE"

echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  All images pushed to $REGISTRY"
echo ""
echo " Next step:  ./scripts/k3s-deploy.sh"
echo "════════════════════════════════════════════════════════════"
