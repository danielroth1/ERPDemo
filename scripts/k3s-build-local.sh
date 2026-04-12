#!/usr/bin/env bash
# =============================================================================
# k3s-build-local.sh
# Builds all ERP Docker images directly into Rancher Desktop's containerd
# image store (namespace k8s.io), making them immediately available to the
# local k3s cluster without any registry push.
#
# Usage (from repo root):
#   ./scripts/k3s-build-local.sh
#
# Then deploy:
#   kubectl apply -k infrastructure/k8s/local/
#   (or use VS Code task: k8s: deploy-local)
#
# Requirements:
#   - Rancher Desktop running (provides nerdctl and the k8s.io containerd namespace)
#
# How it works:
#   nerdctl --namespace k8s.io builds images into the same containerd namespace
#   that Rancher Desktop's k3s kubelet reads. The base manifests use image names
#   like "erp/financial" with imagePullPolicy: IfNotPresent, so once the image
#   exists in the store k8s never tries to pull from a remote registry.
#
#   Local builds use the native host architecture (arm64 on Apple Silicon).
#   Production builds use linux/amd64 via k3s-build-push.sh.
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Services: <image-name>:<context-dir>:<dockerfile>
declare -a SERVICES=(
  "erp/gateway:services:services/gateway/Dockerfile"
  "erp/user-management:services:services/user-management/Dockerfile"
  "erp/inventory:services:services/inventory/Dockerfile"
  "erp/sales:services:services/sales/Dockerfile"
  "erp/financial:services:services/financial/Dockerfile"
  "erp/dashboard:services:services/dashboard/Dockerfile"
  "erp/orchestration:services:services/orchestration/Dockerfile"
  "erp/frontend:frontend:frontend/Dockerfile"
)

echo "════════════════════════════════════════════════════════════"
echo " ERP Local Build (Rancher Desktop / containerd)"
echo " Target : k8s.io containerd namespace"
echo " Arch   : native ($(uname -m))"
echo "════════════════════════════════════════════════════════════"

cd "$REPO_ROOT"

for entry in "${SERVICES[@]}"; do
  IFS=':' read -r image context dockerfile <<< "$entry"
  echo ""
  echo "▶ Building $image"
  echo "  context:    $context"
  echo "  dockerfile: $dockerfile"
  nerdctl --namespace k8s.io build \
    --build-arg RID=linux-arm64 \
    -t "$image" \
    -f "$dockerfile" \
    "$context"
  echo "  ✓ Built $image"
done

echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  All images loaded into containerd (k8s.io namespace)"
echo ""
echo " Next step:  kubectl apply -k infrastructure/k8s/local/"
echo "             (or VS Code task: k8s: deploy-local)"
echo "════════════════════════════════════════════════════════════"
