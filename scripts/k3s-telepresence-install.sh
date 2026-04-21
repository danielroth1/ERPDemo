#!/usr/bin/env bash
# =============================================================================
# Install / upgrade Telepresence Traffic Manager on Contabo k3s (erp-prod namespace)
# =============================================================================
# Prerequisites:
#   - telepresence CLI installed (brew install telepresenceio/telepresence/telepresence-oss)
#   - k3s kubeconfig at ~/.kube/k3s-erp.yaml (created by k3s-setup-server.sh)
#
# Usage:
#   bash scripts/telepresence-install-contabo.sh
# =============================================================================
set -euo pipefail

KUBECONFIG="${KUBECONFIG:-$HOME/.kube/k3s-erp.yaml}"
export KUBECONFIG
NAMESPACE="erp-prod"

echo "▸ Using KUBECONFIG=$KUBECONFIG"
echo "▸ Target namespace: $NAMESPACE"

# Verify prerequisites
if ! command -v telepresence &>/dev/null; then
  echo "✗ telepresence CLI not found. Install with:"
  echo "  brew install telepresenceio/telepresence/telepresence-oss"
  exit 1
fi

if ! kubectl cluster-info &>/dev/null; then
  echo "✗ Cannot reach cluster. Check KUBECONFIG=$KUBECONFIG"
  exit 1
fi

# Ensure namespace exists
if ! kubectl get namespace "$NAMESPACE" &>/dev/null; then
  echo "✗ Namespace '$NAMESPACE' does not exist. Deploy the application first."
  exit 1
fi

# Check if Traffic Manager is already installed
if telepresence helm list --namespace "$NAMESPACE" 2>/dev/null | grep -q traffic-manager; then
  echo "▸ Traffic Manager already installed — upgrading..."
  telepresence helm upgrade \
    --namespace "$NAMESPACE" \
    --set "namespaces={$NAMESPACE}" \
    --reuse-values
else
  echo "▸ Installing Traffic Manager into namespace $NAMESPACE..."
  telepresence helm install \
    --namespace "$NAMESPACE" \
    --set "namespaces={$NAMESPACE}"
fi

echo ""
echo "▸ Verifying Traffic Manager pod..."
kubectl get pods -n "$NAMESPACE" -l app=traffic-manager --no-headers

echo ""
echo "✓ Traffic Manager ready in $NAMESPACE"
echo ""
echo "Next steps:"
echo "  1. Connect:  KUBECONFIG=$KUBECONFIG telepresence connect --namespace $NAMESPACE --manager-namespace $NAMESPACE"
echo "  2. List:     telepresence list"
echo "  3. Intercept: telepresence intercept gateway --port 8080:http --http-header 'x-telepresence-session=yourname'"
