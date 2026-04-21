---
applyTo: "scripts/telepresence-*.sh,.vscode/tasks.json"
---

# Telepresence Instructions

Telepresence lets developers run a service locally while intercepting matching traffic from a remote Kubernetes cluster. It coexists alongside mirrord in this project — use whichever tool you prefer.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  Remote Kubernetes Cluster (AKS / k3s)                         │
│                                                                 │
│  ┌──────────────────┐     ┌──────────────────┐                 │
│  │  Traffic Manager  │◄───►│  Traffic Agent    │  (sidecar,     │
│  │  (Helm-installed) │     │  injected into    │   per-pod)     │
│  └────────┬─────────┘     │  targeted pod)    │                │
│           │               └──────────────────┘                 │
└───────────┼─────────────────────────────────────────────────────┘
            │  gRPC tunnel
┌───────────┼─────────────────────────────────────────────────────┐
│  Developer Workstation                                          │
│           │                                                     │
│  ┌────────▼─────────┐     ┌──────────────────┐                 │
│  │  Root Daemon      │◄───►│  User Daemon      │                │
│  │  (VIF / DNS)      │     │  (session mgmt)   │                │
│  └──────────────────┘     └────────┬─────────┘                 │
│                                    │                            │
│                           ┌────────▼─────────┐                 │
│                           │  Local Service    │                 │
│                           │  (dotnet watch)   │                 │
│                           └──────────────────┘                 │
└─────────────────────────────────────────────────────────────────┘
```

**Traffic Manager** — cluster-side controller installed via Helm. Manages agent injection and proxies traffic between agents and developer workstations.

**Traffic Agent** — sidecar container automatically injected into the targeted pod when an intercept/wiretap/replace starts. Routes or copies matching traffic to your machine.

**Root Daemon** — runs on your workstation with elevated privileges. Manages the virtual network interface (VIF) and DNS overrides so your local process can resolve in-cluster service names.

**User Daemon** — unprivileged process on your workstation. Coordinates intercepts, environment injection, and volume mounts.

## Prerequisites

```bash
# macOS (recommended: package installer for launchd root daemon)
# Download from: https://github.com/telepresenceio/telepresence/releases/latest
# Apple Silicon: telepresence-darwin-arm64.pkg
# Intel:         telepresence-darwin-amd64.pkg

# OR via Homebrew (standalone binary, requires sudo for connect):
brew install telepresenceio/telepresence/telepresence-oss

# Verify
telepresence version
```

After the `.pkg` install on macOS, allow the system extension:
1. Open **System Settings → General → Login Items & Extensions**
2. Find **Tada AB** and enable it

Other prerequisites (should already be installed):
- `kubectl`
- `helm` (used internally by `telepresence helm install`)
- Kubeconfig files: `~/.kube/aks-erp.yaml` (Azure) and/or `~/.kube/k3s-erp.yaml` (Contabo)

## One-Time Cluster Setup

Install the Traffic Manager into each cluster. This only needs to be done once per cluster (or after Telepresence version upgrades).

### Azure (AKS)

```bash
bash scripts/telepresence-install-azure.sh
```

This installs the Traffic Manager into the `erp-azure` namespace, scoped to only that namespace.

### Contabo (k3s)

```bash
bash scripts/telepresence-install-contabo.sh
```

This installs the Traffic Manager into the `erp-prod` namespace, scoped to only that namespace.

### Verify

```bash
# Azure
KUBECONFIG=~/.kube/aks-erp.yaml kubectl get pods -n erp-azure -l app=traffic-manager

# Contabo
KUBECONFIG=~/.kube/k3s-erp.yaml kubectl get pods -n erp-prod -l app=traffic-manager
```

## Daily Workflow

### 1. Connect to the cluster

```bash
# Azure (--also-proxy routes Azure PostgreSQL traffic through the cluster tunnel)
KUBECONFIG=~/.kube/aks-erp.yaml telepresence connect --namespace erp-azure --manager-namespace erp-azure --also-proxy 52.138.158.227/32

# Contabo
KUBECONFIG=~/.kube/k3s-erp.yaml telepresence connect --namespace erp-prod --manager-namespace erp-prod
```

Or use the VS Code tasks: **telepresence: azure: connect** / **telepresence: contabo: connect**

### 2. Intercept a service

```bash
# Example: intercept the gateway service on Azure, filtering by personal header
# (namespace is set at connect time, not on intercept)
telepresence intercept gateway \
  --port 8080:http \
  --http-header 'x-telepresence-session=alice' \
  --env-file /tmp/tp-azure-gateway.env \
  -- dotnet watch run --project services/gateway/ApiGateway/ApiGateway.csproj
```

This:
- Injects a Traffic Agent sidecar into the gateway pod
- Routes only requests containing `x-telepresence-session: alice` to your local machine
- All other traffic continues to reach the cluster pod normally
- Writes the pod's environment variables to `/tmp/tp-azure-gateway.env`
- Starts `dotnet watch run` locally

Or use VS Code tasks: **telepresence: azure: gateway**, **telepresence: azure: user-management**, etc. You'll be prompted for your session key.

### 3. Test your changes

Send requests with your personal header:
```bash
curl -H 'x-telepresence-session: alice' https://erp-azure.mailbase.info/api/users
```

Or configure your frontend/API client to include the header.

### 4. Disconnect

```bash
telepresence leave gateway          # leave a specific intercept
telepresence leave --all            # leave all intercepts
telepresence quit                   # disconnect from cluster entirely
```

Or use VS Code tasks: **telepresence: azure: leave** / **telepresence: contabo: leave**

## VS Code Tasks Reference

All tasks are in `.vscode/tasks.json`. They prompt for a `telepresenceKey` which becomes the `x-telepresence-session` header value.

### Azure Tasks

| Task | Description |
|------|-------------|
| `telepresence: azure: connect` | Connect to AKS cluster |
| `telepresence: azure` | Group: connect + intercept all 7 services |
| `telepresence: azure: gateway` | Intercept gateway (local port 8080) |
| `telepresence: azure: user-management` | Intercept user-management (local port 5001) |
| `telepresence: azure: inventory` | Intercept inventory (local port 5002) |
| `telepresence: azure: sales` | Intercept sales (local port 5003) |
| `telepresence: azure: financial` | Intercept financial (local port 5004) |
| `telepresence: azure: dashboard` | Intercept dashboard (local port 5005) |
| `telepresence: azure: orchestration` | Intercept orchestration (local port 5010) |
| `telepresence: azure: leave` | Leave all intercepts and disconnect |

### Contabo Tasks

Same pattern with `telepresence: contabo:` prefix, targeting `erp-prod` namespace via `~/.kube/k3s-erp.yaml`.

## Multi-Developer Collaboration

Telepresence supports several methods for multiple developers to work on the same cluster simultaneously:

### Option 1: Header-Based Personal Intercepts (Recommended)

Each developer uses a unique header value when intercepting:

```bash
# Alice
telepresence intercept gateway --port 8080:http --http-header 'x-telepresence-session=alice'

# Bob
telepresence intercept gateway --port 8080:http --http-header 'x-telepresence-session=bob'
```

- Alice's local service only receives requests with `x-telepresence-session: alice`
- Bob's local service only receives requests with `x-telepresence-session: bob`
- All other traffic (no header, or different header values) reaches the cluster pod normally
- Multiple developers can intercept the **same service** simultaneously
- The VS Code tasks prompt for a session key — use your name or alias

**This is the recommended approach** and matches the existing mirrord header-filter pattern (`x-mirrord-session`).

### Option 2: Wiretap (Non-Intrusive Observation)

```bash
telepresence wiretap gateway --port 8080:http --http-header 'x-telepresence-session=alice'
```

- Your local machine receives a **copy** of matching traffic
- The remote pod **still processes all requests** — no traffic is stolen
- Multiple developers can wiretap the same service simultaneously
- Useful for debugging and observing production behavior without disruption
- Limitation: your local response changes are not seen by the caller

### Option 3: Path-Based Filtering

```bash
# Alice works on user endpoints
telepresence intercept gateway --port 8080:http --http-path-prefix '/api/users'

# Bob works on inventory endpoints
telepresence intercept gateway --port 8080:http --http-path-prefix '/api/inventory'
```

- Different developers intercept different URL prefixes
- Only viable when developers work on completely separate endpoints
- Header-based intercepts take priority over path-only intercepts

### Option 4: Namespace-Per-Developer

Deploy a full copy of the stack into a personal namespace (e.g., `erp-dev-alice`). Full isolation but high resource overhead. Not recommended for this project unless strict isolation is required.

**Recommendation**: Use **Option 1** (header-based) for active development. Use **Option 2** (wiretap) when you want to observe traffic without affecting the cluster.

## Telepresence vs mirrord

Both tools solve the same problem — running a local service connected to a remote cluster. This project supports both.

| Feature | Telepresence | mirrord |
|---------|-------------|---------|
| Architecture | Traffic Manager (cluster) + client (workstation) | Agent pod (ephemeral, per-session) |
| Install | Requires Helm install of Traffic Manager | No cluster-side install needed |
| Intercept modes | `intercept`, `wiretap`, `replace`, `ingest` | `steal` (with/without filter), `mirror` |
| Header filtering | `--http-header` flag | `http_filter.header_filter` in config JSON |
| Multi-dev support | Native — multiple intercepts per deployment | Via header filters (same deployment) |
| Cluster impact | Persistent Traffic Manager pod | Ephemeral agent pods only |
| DNS/network | VIF + DNS overrides (full cluster connectivity) | Injected into process (no system-wide changes) |
| VS Code integration | CLI-based tasks | CLI-based tasks + VS Code extension available |

**When to use Telepresence**: You want full cluster network connectivity from your workstation (resolve any `*.svc.cluster.local`), need the wiretap mode for non-intrusive debugging, or prefer the well-established CLI workflow.

**When to use mirrord**: You prefer zero cluster-side installation, want per-process isolation (no system-wide DNS changes), or already have mirrord configs set up.

## Troubleshooting

### "Traffic Manager not found"
Run the install script for your cluster:
```bash
bash scripts/telepresence-install-azure.sh   # Azure
bash scripts/telepresence-install-contabo.sh  # Contabo
```

### "Permission denied" on connect
If installed via Homebrew (standalone binary), `telepresence connect` requires sudo. Use the `.pkg` installer instead to set up the root daemon as a launchd service.

### Intercept hangs or fails
```bash
# Check Traffic Manager status
telepresence status

# List available workloads
telepresence list

# Check agent status on a specific pod
kubectl describe pod -n erp-azure -l app=gateway | grep -A5 traffic-agent
```

### Port conflict
If the local port is already in use, the intercept will fail. Stop the conflicting process or change the local port:
```bash
telepresence intercept gateway --port 9080:http ...  # use 9080 instead of 8080
```

### Clean up stuck agents
```bash
telepresence uninstall --agent gateway          # remove agent from specific deployment
telepresence uninstall --all-agents             # remove all agents
```

### Stale connection
```bash
telepresence quit          # disconnect
telepresence connect ...   # reconnect
```
