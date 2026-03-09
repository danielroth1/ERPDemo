# ERP – Production Deployment on k3s

Server: `${K3S_SERVER}`  
Ingress: Traefik (built into k3s, no installation needed)  
Registry: ghcr.io (GitHub Container Registry) or any Docker-compatible registry

---

## Prerequisites (your Mac, one-time)

| Tool | Install |
|---|---|
| `docker` with `buildx` | Already present via Docker Desktop / OrbStack |
| `kubectl` | `brew install kubectl` |
| Registry login | `docker login ghcr.io -u YOUR_GITHUB_USERNAME` (then enter a GitHub PAT with `write:packages` scope) |

---

## Step 0 – One-time: configure your registry

Edit **two files** and replace `ghcr.io/YOURUSER` with your actual value
(e.g. `ghcr.io/daniel`):

- [`scripts/k3s-build-push.sh`](../../scripts/k3s-build-push.sh) – line with `REGISTRY="${REGISTRY:-…}"`
- [`scripts/k3s-deploy.sh`](../../scripts/k3s-deploy.sh) – same line
- create token with scopes here: https://github.com/settings/tokens/new
  - write:packages — push images
  - read:packages — pull images
  - delete:packages — optional, for cleanup
- add token to system: echo TOKEN | docker login ghcr.io -u danielroth1 --password-stdin

Or supply the registry when prompted by the VS Code tasks (no file editing needed).

---

## Step 1 – One-time: set up the server

**VS Code:** `Ctrl+Shift+P` → `Tasks: Run Task` → **`prod: setup-server`**

```bash
# or in a terminal from the repo root:
./scripts/k3s-setup-server.sh
```

**What it does:**
- Verifies that Traefik (k3s built-in ingress) is running on the server
- SSHes in and copies the k3s kubeconfig to `~/.kube/k3s-erp.yaml` on your Mac
- Rewrites the address from `127.0.0.1` to `<k3s-server-ip>` so kubectl can reach it remotely

Run this **once** (or again if the server is rebuilt).

---

## Step 2 – Every release: build & push images

**VS Code:** `Ctrl+Shift+P` → `Tasks: Run Task` → **`prod: build-push`**  
(VS Code will prompt for **Registry** and **Tag**)

```bash
# or in a terminal:
export REGISTRY=ghcr.io/daniel
export TAG=$(git rev-parse --short HEAD)  # or: latest
./scripts/k3s-build-push.sh
```

**What it does:**
- Uses Docker `buildx` to compile all services for `linux/amd64`
  (works even though your Mac is ARM64 – cross-compilation is transparent)
- Pushes 7 images to the registry:
  `erp-gateway`, `erp-user-management`, `erp-inventory`, `erp-sales`,
  `erp-financial`, `erp-dashboard`, `erp-frontend`

The server never builds anything. It only pulls pre-built images.

---

## Step 3 – Every release: deploy to the server

**VS Code:** `Ctrl+Shift+P` → `Tasks: Run Task` → **`prod: deploy`**  
(VS Code will prompt for **Registry** and **Tag**)

```bash
# or in a terminal:
export REGISTRY=ghcr.io/daniel
export TAG=$(git rev-parse --short HEAD)
./scripts/k3s-deploy.sh
```

**What it does:**
- Uses `kubectl` on your Mac to talk to the k3s API on the server (HTTPS port 6443)
- Builds a temporary Kustomize overlay that points to your registry images
- Runs `kubectl apply -k` – the server receives the desired state and pulls the images
- Waits for all 7 deployments to become ready
- Prints the app URL

The app will be reachable at `http://<k3s-server-ip>` when done.

> **Shortcut:** Use **`prod: build-push-deploy`** to run Steps 2 and 3 in one go.

---

## All VS Code tasks (prod: prefix)

Open with `Ctrl+Shift+P` → `Tasks: Run Task`:

| Task | What it does |
|---|---|
| `prod: setup-server` | One-time server setup, copies kubeconfig |
| `prod: build-push` | Build & push all Docker images |
| `prod: deploy` | Deploy to k3s via kubectl |
| `prod: build-push-deploy` | Build + push + deploy in one step |
| `prod: status` | Show pods, ingress, and services in `erp-prod` |
| `prod: logs` | Stream logs for a selected service |
| `prod: rollout-restart` | Force-restart a deployment (re-pulls image) |

---

## Summary: what runs where

```
Your Mac                      Registry (ghcr.io)       k3s Server
────────────────────          ──────────────────        ─────────────────────────
k3s-setup-server.sh  ──ssh──► (copies kubeconfig)
k3s-build-push.sh    ──────►  stores images      ◄────  kubelet pulls images
k3s-deploy.sh        ─kubectl/HTTPS:6443──────────────► k3s API applies manifests
                                                         Traefik routes traffic in
```

Nothing is built on the server. The server only runs containers.

---

## Kubernetes files overview

```
infrastructure/k8s/
├── base/               ← shared manifests (all environments)
│   ├── api-gateway.yaml
│   ├── frontend.yaml
│   ├── postgres.yaml
│   ├── kafka.yaml
│   └── ...
└── production/         ← production-specific layer (this folder)
    ├── kustomization.yaml   ← inherits base, patches service types, sets registry
    ├── namespace.yaml       ← creates the erp-prod namespace
    └── ingress.yaml         ← Traefik ingress rule (routes / → frontend)
```

Kustomize merges `base/` and `production/` at deploy time. You never edit `base/`
for production-specific concerns.

---

## Adding TLS (HTTPS) later

1. Install cert-manager: `./infrastructure/cert-manager/install.sh`
2. Edit [`ingress.yaml`](ingress.yaml) – uncomment the `tls:` block and set your domain
3. Add your domain's A record → `<k3s-server-ip>`
4. Re-run `k3s-deploy.sh`

---

## Useful kubectl commands (after setup)

```bash
export KUBECONFIG=~/.kube/k3s-erp.yaml

kubectl get pods -n erp-prod          # check running pods
kubectl get ingress -n erp-prod        # check ingress rules
kubectl logs -n erp-prod deploy/gateway  # service logs
kubectl rollout restart deploy/frontend -n erp-prod  # force re-pull image
```
