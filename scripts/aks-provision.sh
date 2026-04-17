#!/usr/bin/env bash
# =============================================================================
# aks-provision.sh
# ONE-TIME provisioning of all Azure infrastructure for the ERP system.
#
# Creates:
#   - Resource group
#   - Virtual network + subnet
#   - Azure Container Registry (ACR)
#   - AKS cluster with Key Vault Secrets Provider add-on
#   - ACR ↔ AKS managed identity attachment (no imagePullSecrets needed)
#   - Azure Key Vault + all required secrets
#   - Azure Database for PostgreSQL Flexible Server + 6 databases
#   - Traefik ingress controller (via Helm, free — standard Azure LoadBalancer)
#
# Usage (from repo root):
#   bash scripts/aks-provision.sh
#
# Prerequisites:
#   - Azure CLI logged in: az login
#   - .env.deploy filled with Azure variables (see .env.deploy.example)
#   - Helm installed: brew install helm
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

RG="${AZ_RESOURCE_GROUP}"
LOCATION="${AZ_LOCATION}"
ACR="${AZ_ACR_NAME}"
AKS="${AZ_AKS_NAME}"
KV="${AZ_KEYVAULT_NAME}"
VNET="${AZ_VNET_NAME}"
PG_SERVER="${AZ_PG_SERVER_NAME}"
PG_ADMIN="${AZ_PG_ADMIN_USER}"
PG_PASS="${AZ_PG_ADMIN_PASSWORD}"
PG_LOCATION="${AZ_PG_LOCATION:-${LOCATION}}"  # Falls back to AZ_LOCATION if not set
KUBECONFIG_LOCAL="$HOME/.kube/aks-erp.yaml"
NAMESPACE="erp-azure"

echo "════════════════════════════════════════════════════════════"
echo " ERP Azure Provisioning"
echo " Resource Group : $RG  ($LOCATION)"
echo " AKS            : $AKS"
echo " ACR            : $ACR"
echo " Key Vault      : $KV"
echo " PostgreSQL     : $PG_SERVER"
echo "════════════════════════════════════════════════════════════"

# ── 0. Register required resource providers ───────────────────────────────────
# Free/new subscriptions often have providers unregistered. Registration is
# idempotent and free — safe to run every time.
echo ""
echo "▶ 0/9 — Registering Azure resource providers (once per subscription)..."
for PROVIDER in \
  Microsoft.ContainerRegistry \
  Microsoft.ContainerService \
  Microsoft.KeyVault \
  Microsoft.DBforPostgreSQL \
  Microsoft.Network \
  Microsoft.Compute \
  Microsoft.Storage; do
  az provider register --namespace "$PROVIDER" --wait --output none
  echo "  ✓ $PROVIDER"
done

# ── 1. Resource group ─────────────────────────────────────────────────────────
echo ""
echo "▶ 1/9 — Resource group..."
az group create --name "$RG" --location "$LOCATION" --output none
echo "  ✓ Resource group '$RG' ready"

# ── 2. Virtual network ────────────────────────────────────────────────────────
echo ""
echo "▶ 2/9 — Virtual network..."
if ! az network vnet show --resource-group "$RG" --name "$VNET" --output none 2>/dev/null; then
  az network vnet create \
    --resource-group "$RG" --name "$VNET" \
    --address-prefix 10.0.0.0/8 --output none
fi

if ! az network vnet subnet show --resource-group "$RG" --vnet-name "$VNET" --name aks-subnet --output none 2>/dev/null; then
  az network vnet subnet create \
    --resource-group "$RG" --vnet-name "$VNET" \
    --name aks-subnet --address-prefixes 10.240.0.0/16 --output none
fi

AKS_SUBNET_ID=$(az network vnet subnet show \
  --resource-group "$RG" --vnet-name "$VNET" --name aks-subnet \
  --query id -o tsv)
echo "  ✓ VNet '$VNET' + subnet ready"

# ── 3. Azure Container Registry ───────────────────────────────────────────────
echo ""
echo "▶ 3/9 — Azure Container Registry..."
if ! az acr show --name "$ACR" --resource-group "$RG" --output none 2>/dev/null; then
  az acr create --resource-group "$RG" --name "$ACR" --sku Basic --output none
fi
echo "  ✓ ACR '$ACR.azurecr.io' ready"

# ── 4. AKS cluster ────────────────────────────────────────────────────────────
echo ""
echo "▶ 4/9 — AKS cluster (this takes ~5–8 min)..."
# --node-vm-size Standard_B2s_v2 = 2 vCPU / 4 GB RAM — cheapest allowed on free subscriptions in westeurope.
# Standard_D4s_v3 (4 / 16 GiB) would be a more appropriate choice for production.
# --node-count 1 to save costs
if ! az aks show --resource-group "$RG" --name "$AKS" --output none 2>/dev/null; then
  az aks create \
    --resource-group "$RG" --name "$AKS" \
    --node-count 1 --node-vm-size Standard_B2s_v2 \
    --network-plugin azure --vnet-subnet-id "$AKS_SUBNET_ID" \
    --enable-managed-identity \
    --enable-oidc-issuer \
    --enable-workload-identity \
    --enable-addons azure-keyvault-secrets-provider \
    --enable-secret-rotation \
    --rotation-poll-interval 2m \
    --generate-ssh-keys \
    --output none
  echo "  ✓ AKS cluster '$AKS' created"
else
  echo "  ✓ AKS cluster '$AKS' already exists, skipping"
fi

echo ""
echo "▶ Attaching ACR to AKS (managed identity pull — no imagePullSecrets needed)..."
ACR_ID=$(az acr show --name "$ACR" --resource-group "$RG" --query id -o tsv)
AKS_KUBELET_IDENTITY=$(az aks show --resource-group "$RG" --name "$AKS" \
  --query "identityProfile.kubeletidentity.objectId" -o tsv)
EXISTING_ROLE=$(az role assignment list \
  --assignee "$AKS_KUBELET_IDENTITY" --scope "$ACR_ID" --role "AcrPull" \
  --query "[0].id" -o tsv 2>/dev/null || true)
if [ -z "$EXISTING_ROLE" ]; then
  az aks update --resource-group "$RG" --name "$AKS" --attach-acr "$ACR" --output none
  echo "  ✓ AKS can pull from $ACR.azurecr.io"
else
  echo "  ✓ AKS already has AcrPull on $ACR.azurecr.io, skipping"
fi

echo ""
echo "▶ Fetching kubeconfig..."
mkdir -p "$(dirname "$KUBECONFIG_LOCAL")"
az aks get-credentials --resource-group "$RG" --name "$AKS" \
  --file "$KUBECONFIG_LOCAL" --overwrite-existing
echo "  ✓ Kubeconfig → $KUBECONFIG_LOCAL"

# ── 5. Gateway API CRDs + Traefik v3 ─────────────────────────────────────────
# Kubernetes Gateway API (gateway.networking.k8s.io/v1) replaces Ingress.
# Traefik v3 acts as the gateway controller.
# Standard LoadBalancer is created automatically by Azure — ~€15/month.
echo ""
echo "▶ 5/9 — Gateway API CRDs + Traefik v3 (Helm)..."
export KUBECONFIG="$KUBECONFIG_LOCAL"

# Install Gateway API standard CRDs (GatewayClass, Gateway, HTTPRoute, etc.)
GATEWAY_API_VERSION="v1.2.0"
echo "  Installing Gateway API CRDs $GATEWAY_API_VERSION..."
kubectl apply -f "https://github.com/kubernetes-sigs/gateway-api/releases/download/${GATEWAY_API_VERSION}/standard-install.yaml"
echo "  ✓ Gateway API CRDs installed"

# Install Traefik v3 with Kubernetes Gateway API provider enabled
helm repo add traefik https://traefik.github.io/charts --force-update
helm repo update

helm upgrade --install traefik traefik/traefik \
  --namespace traefik --create-namespace \
  --skip-crds \
  --set providers.kubernetesGateway.enabled=true \
  --set providers.kubernetesIngress.enabled=false \
  --set gateway.enabled=false \
  --set service.type=LoadBalancer \
  --set "service.annotations.service\\.beta\\.kubernetes\\.io/azure-load-balancer-health-probe-request-path"=/ping \
  --set service.externalTrafficPolicy=Local \
  --wait --timeout 5m
echo "  ✓ Traefik v3 installed with Gateway API provider"

TRAEFIK_IP=$(kubectl get service traefik -n traefik \
  -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "pending")
echo "  LoadBalancer IP: $TRAEFIK_IP"
echo "  ⚠  If 'pending', wait 1–2 min then run:"
echo "     KUBECONFIG=~/.kube/aks-erp.yaml kubectl get svc -n traefik"

# ── 6. Azure Key Vault ────────────────────────────────────────────────────────
echo ""
echo "▶ 6/9 — Azure Key Vault..."
if ! az keyvault show --name "$KV" --resource-group "$RG" --output none 2>/dev/null; then
  az keyvault create \
    --resource-group "$RG" --name "$KV" \
    --location "$LOCATION" --sku standard \
    --enable-rbac-authorization false \
    --output none
fi

# Grant the AKS Key Vault Secrets Provider managed identity GET+LIST on secrets
KV_IDENTITY_CLIENT_ID=$(az aks show \
  --resource-group "$RG" --name "$AKS" \
  --query "addonProfiles.azureKeyvaultSecretsProvider.identity.clientId" -o tsv)

# Use object-id (more reliable than --spn for managed identities)
KV_IDENTITY_OBJECT_ID=$(az ad sp show --id "$KV_IDENTITY_CLIENT_ID" --query id -o tsv)

az keyvault set-policy --name "$KV" \
  --object-id "$KV_IDENTITY_OBJECT_ID" \
  --secret-permissions get list \
  --output none

echo "  ✓ Key Vault '$KV' ready"
echo "  Key Vault Identity Client ID: $KV_IDENTITY_CLIENT_ID"

# Create federated identity credential so the CSI Secrets Store driver can
# authenticate to Key Vault using workload identity (OIDC token exchange).
NODE_RG=$(az aks show -g "$RG" -n "$AKS" --query nodeResourceGroup -o tsv)
OIDC_ISSUER=$(az aks show -g "$RG" -n "$AKS" --query "oidcIssuerProfile.issuerUrl" -o tsv)
KV_IDENTITY_NAME="azurekeyvaultsecretsprovider-${AKS}"

if ! az identity federated-credential show \
  --name "keyvault-federated-credential-${NAMESPACE}" \
  --identity-name "$KV_IDENTITY_NAME" \
  --resource-group "$NODE_RG" --output none 2>/dev/null; then
  az identity federated-credential create \
    --name "keyvault-federated-credential-${NAMESPACE}" \
    --identity-name "$KV_IDENTITY_NAME" \
    --resource-group "$NODE_RG" \
    --issuer "$OIDC_ISSUER" \
    --subject "system:serviceaccount:${NAMESPACE}:default" \
    --audiences "api://AzureADTokenExchange" \
    --output none
  echo "  ✓ Federated identity credential created for ${NAMESPACE}:default"
else
  echo "  ✓ Federated identity credential already exists, skipping"
fi

# ── 7. Azure Database for PostgreSQL Flexible Server ──────────────────────────
echo ""
echo "▶ 7/9 — PostgreSQL Flexible Server (this takes ~3–5 min)..."
# --sku-name Standard_B1ms = Azure Database for PostgreSQL Basic tier, 1 vCore, 2 GB RAM (cheapest option, free for new accounts)
if ! az postgres flexible-server show --resource-group "$RG" --name "$PG_SERVER" --output none 2>/dev/null; then
  az postgres flexible-server create \
    --resource-group "$RG" --name "$PG_SERVER" \
    --location "$PG_LOCATION" \
    --admin-user "$PG_ADMIN" --admin-password "$PG_PASS" \
    --sku-name Standard_B1ms --tier Burstable \
    --storage-size 32 --version 16 \
    --public-access 0.0.0.0 \
    --output none
fi

echo "▶ Creating ERP databases..."
for DB in erp_users erp_inventory erp_sales erp_financial erp_dashboard erp_orchestration; do
  if ! az postgres flexible-server db show --resource-group "$RG" --server-name "$PG_SERVER" --database-name "$DB" --output none 2>/dev/null; then
    az postgres flexible-server db create \
      --resource-group "$RG" --server-name "$PG_SERVER" \
      --database-name "$DB" --output none
    echo "  ✓ $DB created"
  else
    echo "  ✓ $DB already exists, skipping"
  fi
done

PG_HOST="${PG_SERVER}.postgres.database.azure.com"
echo "  ✓ PostgreSQL host: $PG_HOST"

# ── 8. Store secrets in Key Vault ─────────────────────────────────────────────
echo ""
echo "▶ 8/9 — Populating Key Vault secrets..."

PG_SSL="Ssl Mode=Require;Trust Server Certificate=true"
PG_BASE="Host=${PG_HOST};Port=5432;Username=${PG_ADMIN};Password=${PG_PASS};${PG_SSL}"

az keyvault secret set --vault-name "$KV" --name "pg-connection-string-users"         --value "${PG_BASE};Database=erp_users"         --output none
az keyvault secret set --vault-name "$KV" --name "pg-connection-string-inventory"     --value "${PG_BASE};Database=erp_inventory"     --output none
az keyvault secret set --vault-name "$KV" --name "pg-connection-string-sales"         --value "${PG_BASE};Database=erp_sales"         --output none
az keyvault secret set --vault-name "$KV" --name "pg-connection-string-financial"     --value "${PG_BASE};Database=erp_financial"     --output none
az keyvault secret set --vault-name "$KV" --name "pg-connection-string-dashboard"     --value "${PG_BASE};Database=erp_dashboard"     --output none
az keyvault secret set --vault-name "$KV" --name "pg-connection-string-orchestration" --value "${PG_BASE};Database=erp_orchestration" --output none
az keyvault secret set --vault-name "$KV" --name "grafana-admin-user"                 --value "admin"                                 --output none
az keyvault secret set --vault-name "$KV" --name "grafana-admin-password"             --value "${GRAFANA_ADMIN_PASSWORD}"             --output none

echo "  ✓ All secrets stored in Key Vault"

# ── Print summary ─────────────────────────────────────────────────────────────
TENANT_ID=$(az account show --query tenantId -o tsv)

echo ""
echo "════════════════════════════════════════════════════════════"
echo " ✅  Provisioning complete!"
echo ""
echo "  LoadBalancer IP : ${TRAEFIK_IP}  (re-check with: azure: status task)"
echo "  PostgreSQL host : ${PG_HOST}"
echo ""
echo "  ── Add these to your .env.deploy ──────────────────────────"
echo "  AZURE_TENANT_ID=${TENANT_ID}"
echo "  AZURE_KV_IDENTITY_CLIENT_ID=${KV_IDENTITY_CLIENT_ID}"
echo "  AZ_PG_HOST=${PG_HOST}"
echo ""
echo "  ── Then run (in order) ─────────────────────────────────────"
echo "  1. Point DNS A record:  AZURE_DOMAIN → ${TRAEFIK_IP}"
echo "  2. bash infrastructure/cert-manager/install-azure.sh"
echo "  3. bash scripts/aks-build-push.sh"
echo "  4. bash scripts/aks-deploy.sh"
echo "════════════════════════════════════════════════════════════"
