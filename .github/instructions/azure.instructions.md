---
applyTo: "scripts/aks-*.sh,infrastructure/k8s/azure/**,infrastructure/cert-manager/install-azure.sh"
---

# Azure / AKS Instructions

## Architecture

| Component          | Azure Service                                      | Notes                                    |
|--------------------|----------------------------------------------------|------------------------------------------|
| Kubernetes         | AKS (Azure Kubernetes Service)                     | `~/.kube/aks-erp.yaml`                   |
| Container Registry | Azure Container Registry (ACR)                     | Managed identity pull — no pull secret   |
| Ingress            | Traefik v3 + Kubernetes Gateway API (HTTPRoute)    | Azure Standard LB ~€15/month             |
| TLS                | cert-manager + Let's Encrypt                       | Auto-renewed, Gateway API http01 solver  |
| Database           | Azure Database for PostgreSQL Flexible Server      | Replaces self-hosted Postgres StatefulSet|
| Secrets            | Azure Key Vault + Secret Store CSI Driver          | `grafana-admin-secret` synced via CSI    |
| Message Queue      | Self-hosted Kafka container (unchanged from base)  | Kafka PVC uses Azure Disk (managed-csi)  |
| Monitoring         | Self-hosted Prometheus, Grafana, Loki, Alloy       | Unchanged from base                      |
| Kubeconfig         | `~/.kube/aks-erp.yaml`                             | Separate from k3s (`k3s-erp.yaml`)       |

## Kustomize Overlay Structure

```
infrastructure/k8s/
  base/             ← shared manifests (unchanged — local + k3s still work)
  local/            ← local Docker Desktop k8s (unchanged)
  production/       ← k3s server at <k3s-server-ip> (unchanged)
  azure/            ← AKS overlay (this file's scope)
    kustomization.yaml
    namespace.yaml
    gateway.yaml            Traefik Gateway API resources, domain injected at deploy time
    secret-provider.yaml   SecretProviderClass + secret-sync deployment
    patches/
      remove-postgres-wait.yaml   removes wait-for-postgres init container
```

**What differs from base in the Azure overlay:**
- `postgres` StatefulSet scaled to 0 — Azure DB Flexible Server used instead
- `wait-for-postgres` init containers removed from 5 services
- Images reference ACR (`erpacr.azurecr.io/...`) instead of local `erp/*`
- `imagePullPolicy: Always` for all Deployments (ACR managed identity)
- `frontend-service`, `gateway-service`, `grafana-service`, `prometheus-service` patched to `ClusterIP`
- Grafana patched to read admin credentials from `grafana-admin-secret` (Key Vault)
- Traefik Gateway API (Gateway + HTTPRoute resources), hosts injected at deploy time

## Prerequisites

```bash
brew install azure-cli   # az login afterwards
brew install helm        # for Traefik v3 + cert-manager Helm installs
brew install kubectl     # should already be present
```

## First-Time Setup (run once per environment)

```bash
# 1. Fill in ALL Azure variables in .env.deploy (see .env.deploy.example)

# 2. Provision everything (AKS, ACR, Key Vault, PostgreSQL, Traefik Gateway API)
bash scripts/aks-provision.sh

# 3. Copy the 3 values printed at the end into .env.deploy:
#    AZURE_TENANT_ID=...
#    AZURE_KV_IDENTITY_CLIENT_ID=...
#    AZ_PG_HOST=...

# 4. Point DNS A records to the LoadBalancer IP printed by provision script:
#    AZURE_DOMAIN        → <LoadBalancer IP>
#    www.AZURE_DOMAIN    → <LoadBalancer IP>
#    monitoring.AZURE_DOMAIN → <LoadBalancer IP>

# 5. Install cert-manager (one-time)
KUBECONFIG=~/.kube/aks-erp.yaml bash infrastructure/cert-manager/install-azure.sh

# 6. Build and push images to ACR
bash scripts/aks-build-push.sh

# 7. Deploy to AKS
bash scripts/aks-deploy.sh
```

## Day-to-Day Workflow

```bash
# Build + push + deploy in one command
bash scripts/aks-build-push.sh && bash scripts/aks-deploy.sh

# Or use VS Code task:  azure: build-push-deploy
```

## Kubeconfig

```bash
# AKS cluster
export KUBECONFIG=~/.kube/aks-erp.yaml

# Revert to k3s (production)
export KUBECONFIG=~/.kube/k3s-erp.yaml
```

The deploy scripts and VS Code tasks set `KUBECONFIG` automatically — you don't need to export it manually when using them.

## Secrets

### Overview

| Secret                | Source                         | How created                                         |
|-----------------------|--------------------------------|-----------------------------------------------------|
| `postgres-secret`     | Azure Key Vault (via .env.deploy) | `aks-deploy.sh` Phase 3 (imperative kubectl)     |
| `grafana-admin-secret`| Azure Key Vault (CSI driver)   | `secret-sync` pod → `SecretProviderClass` sync      |
| `rabbitmq-secret`     | Base manifest                  | `guest/guest` (self-hosted RabbitMQ, unchanged)     |

### Rotating a secret

```bash
# 1. Update value in Key Vault
az keyvault secret set \
  --vault-name $AZ_KEYVAULT_NAME \
  --name grafana-admin-password \
  --value "new-password"

# CSI driver picks up the new value within 2 minutes (rotation-poll-interval).
# Restart Grafana to apply:
KUBECONFIG=~/.kube/aks-erp.yaml \
  kubectl rollout restart deployment/grafana -n erp-azure
```

For `postgres-secret`, update `AZ_PG_ADMIN_PASSWORD` in `.env.deploy` and re-run `aks-deploy.sh`.

### Adding a new Key Vault secret

1. Add to Key Vault:
   ```bash
   az keyvault secret set --vault-name $AZ_KEYVAULT_NAME --name my-secret --value "value"
   ```
2. Add the `objectName` entry to `spec.parameters.objects` in `infrastructure/k8s/azure/secret-provider.yaml`
3. Add the mapping to `spec.secretObjects` in the same file
4. Run `aks-deploy.sh`

## Cost Breakdown (approximate, West Europe)

| Resource                            | Cost/month |
|-------------------------------------|------------|
| AKS (3× Standard_D4s_v3 nodes)     | ~€330      |
| Azure Standard LoadBalancer (Traefik) | ~€15       |
| Azure Container Registry (Basic)    | ~€4        |
| Azure Database PostgreSQL (Burstable B2ms) | ~€30  |
| Azure Key Vault                     | ~€1        |
| **Total**                           | **~€380**  |

Scale AKS nodes down when not in use (`azure: scale-nodes` task).

## Troubleshooting

```bash
# Check pod status
KUBECONFIG=~/.kube/aks-erp.yaml kubectl get pods -n erp-azure

# Check Gateway and HTTPRoute status
KUBECONFIG=~/.kube/aks-erp.yaml kubectl get gateway,httproute -n erp-azure

# Check secret-sync (Key Vault CSI connectivity)
KUBECONFIG=~/.kube/aks-erp.yaml \
  kubectl describe pod -l app=secret-sync -n erp-azure

# Check if grafana-admin-secret was created
KUBECONFIG=~/.kube/aks-erp.yaml \
  kubectl get secrets -n erp-azure

# Check cert-manager certificate issuance
KUBECONFIG=~/.kube/aks-erp.yaml \
  kubectl get certificate,certificaterequest -n erp-azure -w

# Check Traefik events / logs
KUBECONFIG=~/.kube/aks-erp.yaml \
  kubectl logs -n traefik deploy/traefik --tail=50

# Check Traefik LoadBalancer IP
KUBECONFIG=~/.kube/aks-erp.yaml \
  kubectl get svc -n traefik

# Stream ERP service logs
KUBECONFIG=~/.kube/aks-erp.yaml \
  kubectl logs -n erp-azure -f deploy/<service-name>
```

## Notes

- **JWT secret** (`Jwt__Secret`) is currently hardcoded in the base manifests, same as the k3s production deployment. Storing it in Key Vault requires per-service patches; deferred to a future improvement.
- **Kafka storage**: uses the default AKS `StorageClass` (`managed-csi`, Azure Disk). No additional configuration needed.
- **`local/` and `production/` overlays are unaffected** — they continue to work independently.
