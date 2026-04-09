---
applyTo: "services/**/Generated/**,docs/openapi/**,scripts/update-api-clients.mjs"
---

# Backend API Client Workflow

This project uses [Kiota](https://learn.microsoft.com/en-us/openapi/kiota/overview) to generate strongly-typed C# HTTP clients for inter-service communication. The generated clients live alongside the consumer service and are **committed to the repository**.

## How it works

Generation is a two-phase process driven by committed OpenAPI spec files — no running services are required for generation itself.

```
┌─────────────────────────────────────────────────────────────┐
│ Phase 1 – Export (requires running services)                │
│  Fetch swagger.json from each service → docs/openapi/*.json  │
└──────────────────────────────┬──────────────────────────────┘
                               │ committed to git
┌──────────────────────────────▼──────────────────────────────┐
│ Phase 2 – Generate (no running services needed)             │
│  dotnet kiota generate … → services/<svc>/Generated/        │
└─────────────────────────────────────────────────────────────┘
```

### Provider → Consumer mapping

| Provider spec (`docs/openapi/`) | Consumed by |
|---|---|
| `financial.json` | `inventory`, `sales`, `dashboard` |
| `inventory.json` | `sales`, `dashboard` |
| `sales.json` | `dashboard` |
| `user-management.json` | `dashboard` |

### Generated client locations

| Consumer service | Output path |
|---|---|
| `inventory` | `services/inventory/InventoryManagement/Generated/Clients/` |
| `sales` | `services/sales/SalesManagement/Generated/Clients/` |
| `dashboard` | `services/dashboard/DashboardAnalytics/Generated/Clients/` |

## When to update clients

Run the update workflow whenever:

- You **add, remove, or change an endpoint** in a provider service (financial, inventory, sales, user-management)
- You **change a request/response model** that crosses service boundaries
- The Kiota tool version is bumped in `.config/dotnet-tools.json`

## How to update

### VS Code tasks (recommended)

| Task | Description |
|---|---|
| `backend: update-all-api-clients` | Update all three consumer services at once |
| `backend: update-api-clients` | Pick a single consumer (`inventory`, `sales`, or `dashboard`) |

Both tasks run both phases automatically. The required provider services must be running (use `backend: watch-all-services`).

### CLI

```bash
# All consumer services
node scripts/update-api-clients.mjs

# Single consumer service
node scripts/update-api-clients.mjs --service inventory
node scripts/update-api-clients.mjs --service sales
node scripts/update-api-clients.mjs --service dashboard
```

### What to commit

After running an update, commit together:
1. Updated spec file(s) in `docs/openapi/`
2. Regenerated client files in `services/<svc>/Generated/Clients/`
3. Updated `kiota-lock.json` files (if Kiota version or config changed)

## CI/CD

The `.github/workflows/ci-cd.yml` `test-backend` job runs `dotnet kiota generate` automatically before `dotnet restore` for `inventory`, `sales`, and `dashboard`, reading directly from the committed `docs/openapi/` specs. No running services are needed in CI.

## Adding a new inter-service dependency

1. Add the provider's spec to `docs/openapi/` by running the update script with the services running.
2. Add an entry to the `CONSUMERS` map in `scripts/update-api-clients.mjs`.
3. Add a corresponding `dotnet kiota generate` step to the relevant CI job in `.github/workflows/ci-cd.yml`.
4. Inject and use the generated client via DI in the consumer service (see existing `FinancialServiceClientWrapper` as a reference).

## `.gitignore` rules

Generated **C# source files** (`Generated/**/*.cs`) are excluded from git — they are always regenerated. The **`kiota-lock.json`** files inside each `Generated/Clients/<Name>/` directory are **tracked** so CI knows how to regenerate them.

```
# services/<svc>/.gitignore
Generated/**/*.cs       # excluded – always regenerated
*.kiota.log             # excluded – local log only
# kiota-lock.json is NOT ignored – tracked for CI
```
