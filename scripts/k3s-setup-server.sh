#!/usr/bin/env bash
# =============================================================================
# k3s-setup-server.sh
# One-time setup for the remote k3s server at ${K3S_SERVER}
#
# What this does:
#   1. Verifies Traefik (k3s built-in ingress) is running
#   2. Copies the k3s kubeconfig to ~/.kube/k3s-erp.yaml on your Mac
#
# Run once from your Mac:
#   chmod +x scripts/k3s-setup-server.sh
#   ./scripts/k3s-setup-server.sh
# =============================================================================
set -euo pipefail

# ── Load local deploy config ─────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/../.env.deploy"
if [ ! -f "$ENV_FILE" ]; then
    echo "❌ Missing .env.deploy — copy .env.deploy.example and fill in your values"
    exit 1
fi
# shellcheck source=../.env.deploy
source "$ENV_FILE"

SERVER="${K3S_SERVER}"
SSH_PORT="${K3S_SSH_PORT}"
KUBECONFIG_LOCAL="$HOME/.kube/k3s-erp.yaml"

echo "════════════════════════════════════════════════════════════"
echo " ERP k3s Server Setup"
echo " Server: $SERVER"
echo "════════════════════════════════════════════════════════════"

# ── Step 1: Verify Traefik is running (k3s ships it by default) ───────────────
echo ""
echo "▶ Step 1/3 – Verifying Traefik ingress controller..."
ssh -p "$SSH_PORT" "$SERVER" bash <<'REMOTE'
set -euo pipefail
kubectl wait --for=condition=ready pod \
  -l app.kubernetes.io/name=traefik \
  -n kube-system \
  --timeout=60s
echo "Traefik pods:"
kubectl get pods -n kube-system -l app.kubernetes.io/name=traefik
REMOTE
echo "  ✓ Traefik is running"

# ── Step 2: Copy kubeconfig to local machine ──────────────────────────────────
echo ""
echo "▶ Step 2/3 – Fetching kubeconfig..."
mkdir -p "$(dirname "$KUBECONFIG_LOCAL")"

# Copy /etc/rancher/k3s/k3s.yaml and replace only the hostname (preserve whatever port k3s uses)
# K3S_SERVER is "user@host" — extract just the host part
K3S_HOST="${K3S_SERVER#*@}"
ssh -p "$SSH_PORT" "$SERVER" "cat /etc/rancher/k3s/k3s.yaml" \
  | sed "s|https://127.0.0.1:|https://${K3S_HOST}:|g" \
  > "$KUBECONFIG_LOCAL"

chmod 600 "$KUBECONFIG_LOCAL"
echo "  ✓ Kubeconfig saved to $KUBECONFIG_LOCAL"
echo ""
echo "  To use this cluster:  export KUBECONFIG=$KUBECONFIG_LOCAL"
echo "  Or merge with main:   KUBECONFIG=~/.kube/config:$KUBECONFIG_LOCAL kubectl config view --flatten > /tmp/merged.yaml && mv /tmp/merged.yaml ~/.kube/config"

# ── Step 3: Verify ────────────────────────────────────────────────────────────
echo ""
echo "▶ Step 3/3 – Verifying cluster access from local machine..."
KUBECONFIG="$KUBECONFIG_LOCAL" kubectl get nodes
echo ""
echo "  Traefik ingress pods:"
KUBECONFIG="$KUBECONFIG_LOCAL" kubectl get pods -n kube-system -l app.kubernetes.io/name=traefik

echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  Setup complete!"
echo ""
echo " Next steps:"
echo "   1. Edit kustomization.yaml – set your registry prefix"
echo "   2. Build & push images:  ./scripts/k3s-build-push.sh"
echo "   3. Deploy the app:       ./scripts/k3s-deploy.sh"
echo "════════════════════════════════════════════════════════════"
