---
applyTo: "tests/e2e/**"
---

# E2E Tests (Playwright)

All end-to-end tests live under `tests/e2e/`. They exercise the entire system (infra + all backend services + frontend) through a real browser. Tests run against the same dev stack that developers use locally.

## Project layout

```
tests/e2e/
├── playwright.config.ts        ← config: baseURL, projects (chromium/firefox/webkit), auth setup
├── global-setup.ts             ← health-checks frontend + gateway before tests run
├── package.json
├── tsconfig.json
├── helpers/
│   ├── auth.ts                 ← UI login/logout/register helpers (fill form, click submit)
│   ├── api.ts                  ← direct API calls (seed data, call gateway as authenticated user)
│   └── fixtures.ts             ← custom Playwright fixtures; exports TEST_USER credentials
└── tests/
    ├── auth.setup.ts           ← Playwright setup project: logs in, saves storageState
    ├── auth.spec.ts
    ├── navigation.spec.ts
    ├── inventory.spec.ts
    ├── users.spec.ts
    ├── sales.spec.ts
    ├── financial.spec.ts
    ├── analytics.spec.ts
    ├── shop.spec.ts
    └── database.spec.ts
```

## Prerequisites before running

The full dev stack must be running:
1. Infra (Postgres, Kafka, RabbitMQ): `docker compose -f infrastructure/docker-compose.dev.yml up -d`
2. All backend services: run VS Code task **backend: watch-all-services**
3. Frontend dev server: run VS Code task **frontend: dev**

Or use the composite task **dev-setup**.

`global-setup.ts` hits `/health/ready` on the gateway and the frontend origin. Tests fail fast with a clear message if the stack is not running — do not add manual waits or retry loops to work around a missing stack.

## How to run

```bash
cd tests/e2e

# All tests, Chromium only (fastest for local dev)
npx playwright test --project=chromium

# All browsers
npx playwright test

# Single spec file
npx playwright test tests/inventory.spec.ts --project=chromium

# Interactive UI mode (recommended for development)
npx playwright test --ui

# Headed (watch the browser)
npx playwright test --headed --project=chromium
```

Or use the VS Code tasks:
- **e2e: test** — Chromium only
- **e2e: test:all-browsers** — all three browsers
- **e2e: test:ui** — Playwright UI mode

## Authentication strategy

Tests run as an authenticated user to avoid logging in before every test:

1. `tests/auth.setup.ts` runs first (it is the `setup` project in `playwright.config.ts`).
2. It logs in via the UI and saves the resulting browser storage state to `playwright/.auth/user.json`.
3. The `chromium`, `firefox`, and `webkit` projects load that file via `storageState`, so every test starts already authenticated.
4. `playwright/.auth/` is git-ignored — it is regenerated on every test run.
5. Override test credentials via env vars: `E2E_USER_EMAIL` / `E2E_USER_PASSWORD` (defaults: `admin@erp.com` / `Admin123!`).

## Writing a new spec file

1. Create `tests/e2e/tests/<feature>.spec.ts`.
2. Import `test` and `expect` directly from `@playwright/test` — the `storageState` from `playwright.config.ts` provides an authenticated page automatically.
3. Use `test.beforeEach` to navigate to the feature route.
4. Prefer user-facing locators: `getByRole`, `getByText`, `getByLabel`, `getByPlaceholder`. Avoid positional selectors like `nth()` or CSS class selectors.
5. Use `await expect(locator).toBeVisible()` — not `waitForSelector` — and always provide a reasonable `timeout` option for content that depends on an API call (10–15 s).

```typescript
import { test, expect } from '@playwright/test';

test.describe('My Feature', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/my-feature');
    await expect(page.getByText('My Feature Title')).toBeVisible();
  });

  test('shows the page title', async ({ page }) => {
    await expect(page.getByText('My Feature Title')).toBeVisible();
  });

  test('table renders data or empty state', async ({ page }) => {
    const table = page.locator('table');
    const empty = page.getByText(/no items/i);
    await expect(table.or(empty).first()).toBeVisible({ timeout: 15_000 });
  });
});
```

## Seeding test data via the API

Use `apiRequest` from `helpers/api.ts` inside a test to create data before asserting on it. Prefer this over relying on pre-existing database content so tests are deterministic.

```typescript
import { test, expect } from '@playwright/test';
import { apiRequest } from '../helpers/api';

test('newly created item appears in the list', async ({ page }) => {
  // Seed a product via the gateway API
  await apiRequest(page, 'POST', '/api/v1/products', {
    name: 'Test Widget',
    sku: 'TST-001',
    unitPrice: 9.99,
    stockQuantity: 10,
  });

  await page.goto('/inventory');
  await expect(page.getByText('Test Widget')).toBeVisible({ timeout: 10_000 });
});
```

## Auth-sensitive tests (no storageState)

Tests that must run unauthenticated (e.g., login form validation) must override the default `storageState`:

```typescript
test.use({ storageState: { cookies: [], origins: [] } });
```

Place this call at the top of the `describe` block or spec file, before any `test()` calls.

## Environment variables

| Variable | Default | Purpose |
|---|---|---|
| `BASE_URL` | `http://localhost:5173` | Frontend origin |
| `GATEWAY_URL` | `http://localhost:8080` | API gateway origin (used by helpers/api.ts and global-setup.ts) |
| `E2E_USER_EMAIL` | `admin@erp.com` | Login credential for the setup project |
| `E2E_USER_PASSWORD` | `Admin123!` | Login credential for the setup project |

## Installing browsers

Run once after cloning or after upgrading `@playwright/test`:

```bash
cd tests/e2e
npx playwright install --with-deps chromium firefox webkit
# or via VS Code task: e2e: install-browsers
```

## Known pitfalls

- **Stack not running**: `global-setup.ts` fails fast with a descriptive error. Start the dev stack first.
- **`playwright/.auth/user.json` missing**: The `setup` project creates it. If you skip the setup project (e.g. `--project=chromium` without running setup first), tests will fail because there is no auth state. Run `npx playwright test` (without `--project`) at least once to generate it.
- **Flaky timing**: If an assertion fails intermittently, increase the `timeout` option on the `expect` call (e.g. `{ timeout: 15_000 }`). Do not add `page.waitForTimeout()` — use assertion-based waits instead.
- **Test isolation**: E2E tests share a real database. If a test creates data, subsequent tests may see it. Design assertions to be resilient to extra rows (e.g. check that *at least one* row exists, not that *exactly N* rows exist), or clean up via API in `afterEach`.
