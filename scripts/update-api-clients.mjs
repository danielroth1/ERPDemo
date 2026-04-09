#!/usr/bin/env node
/**
 * Update backend API clients (two-phase, no shell dependencies):
 *   1. Export OpenAPI specs from running local services → docs/openapi/
 *   2. Regenerate Kiota C# clients from the saved specs
 *
 * Works on Windows, macOS, and Linux.
 *
 * Usage:
 *   node scripts/update-api-clients.mjs                    # all consumer services
 *   node scripts/update-api-clients.mjs --service inventory
 *   node scripts/update-api-clients.mjs --service sales
 *   node scripts/update-api-clients.mjs --service dashboard
 */

import { exec } from 'node:child_process';
import { promisify } from 'node:util';
import { createWriteStream, mkdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import http from 'node:http';

const execAsync = promisify(exec);
const __dirname = dirname(fileURLToPath(import.meta.url));
const ROOT = join(__dirname, '..');

// ── Colours ───────────────────────────────────────────────────────────────────

const c = {
  reset:  '\x1b[0m',
  cyan:   '\x1b[36m',
  green:  '\x1b[32m',
  yellow: '\x1b[33m',
  red:    '\x1b[31m',
  gray:   '\x1b[90m',
};

const log = (msg, color = c.reset) => console.log(`${color}${msg}${c.reset}`);

// ── Config ────────────────────────────────────────────────────────────────────

/**
 * Services that expose an OpenAPI spec. Only the ones needed by the selected
 * consumer(s) are fetched during the export phase.
 */
const PROVIDERS = {
  'user-management': 5001,
  'inventory':       5002,
  'sales':           5003,
  'financial':       5004,
  'dashboard':       5005,
  'orchestration':   5010,
};

/**
 * Consumer service → list of Kiota client generation entries.
 * Each entry maps a provider spec to an output directory + class/namespace.
 */
const CONSUMERS = {
  inventory: [
    {
      spec:      'financial',
      output:    'services/inventory/InventoryManagement/Generated/Clients/Financial',
      className: 'FinancialServiceClient',
      namespace: 'InventoryManagement.Generated.Clients.Financial',
    },
  ],
  sales: [
    {
      spec:      'financial',
      output:    'services/sales/SalesManagement/Generated/Clients/Financial',
      className: 'FinancialServiceClient',
      namespace: 'SalesManagement.Generated.Clients.Financial',
    },
    {
      spec:      'inventory',
      output:    'services/sales/SalesManagement/Generated/Clients/Inventory',
      className: 'InventoryServiceClient',
      namespace: 'SalesManagement.Generated.Clients.Inventory',
    },
  ],
  dashboard: [
    {
      spec:      'user-management',
      output:    'services/dashboard/DashboardAnalytics/Generated/Clients/UserManagement',
      className: 'UserManagementServiceClient',
      namespace: 'DashboardAnalytics.Generated.Clients.UserManagement',
    },
    {
      spec:      'sales',
      output:    'services/dashboard/DashboardAnalytics/Generated/Clients/Sales',
      className: 'SalesServiceClient',
      namespace: 'DashboardAnalytics.Generated.Clients.Sales',
    },
    {
      spec:      'financial',
      output:    'services/dashboard/DashboardAnalytics/Generated/Clients/Financial',
      className: 'FinancialServiceClient',
      namespace: 'DashboardAnalytics.Generated.Clients.Financial',
    },
    {
      spec:      'inventory',
      output:    'services/dashboard/DashboardAnalytics/Generated/Clients/Inventory',
      className: 'InventoryServiceClient',
      namespace: 'DashboardAnalytics.Generated.Clients.Inventory',
    },
  ],
};

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Fetch a URL and stream the response body to a local file. */
function fetchToFile(url, destPath) {
  return new Promise((resolve, reject) => {
    const req = http.get(url, { timeout: 5000 }, (res) => {
      if (res.statusCode < 200 || res.statusCode >= 300) {
        res.resume();
        return reject(new Error(`HTTP ${res.statusCode}`));
      }
      const out = createWriteStream(destPath);
      res.pipe(out);
      out.on('finish', () => out.close(resolve));
      out.on('error', reject);
    });
    req.on('error', reject);
    req.on('timeout', () => { req.destroy(); reject(new Error('connection timeout')); });
  });
}

/** Collect the set of provider spec names required by the given consumers. */
function requiredSpecs(consumers) {
  return new Set(Object.values(consumers).flat().map(e => e.spec));
}

// ── Phase 1: Export OpenAPI specs ─────────────────────────────────────────────

async function exportSpecs(consumers) {
  const needed = requiredSpecs(consumers);
  const dir = join(ROOT, 'docs', 'openapi');
  mkdirSync(dir, { recursive: true });

  log('\nPhase 1 – Exporting OpenAPI specs from running services', c.cyan);

  let failures = 0;
  for (const name of needed) {
    const port = PROVIDERS[name];
    const url  = `http://localhost:${port}/swagger/v1/swagger.json`;
    const dest = join(dir, `${name}.json`);
    try {
      await fetchToFile(url, dest);
      log(`  ✓ ${name}`, c.green);
    } catch (err) {
      log(`  ✗ ${name}  (${err.message})`, c.red);
      failures++;
    }
  }

  if (failures > 0) {
    log(
      `\n  ${failures} service(s) were not reachable.` +
      `  Make sure all required services are running first.\n` +
      `  Tip: use the "backend: watch-all-services" task in VS Code.\n`,
      c.yellow,
    );
    process.exit(1);
  }
}

// ── Phase 2: Regenerate Kiota C# clients ──────────────────────────────────────

async function generateClients(consumers) {
  log('\nPhase 2 – Regenerating Kiota C# clients', c.cyan);

  log('  Restoring .NET tools…', c.gray);
  await execAsync('dotnet tool restore', { cwd: ROOT });

  for (const [consumer, entries] of Object.entries(consumers)) {
    log(`\n  Consumer: ${consumer}`, c.cyan);
    for (const entry of entries) {
      const specPath   = join(ROOT, 'docs', 'openapi', `${entry.spec}.json`);
      const outputPath = join(ROOT, entry.output);

      // Build the command as an array to avoid quoting issues on all platforms
      const args = [
        'dotnet', 'kiota', 'generate',
        '--language', 'CSharp',
        '--openapi', specPath,
        '--output', outputPath,
        '--class-name', entry.className,
        '--namespace-name', entry.namespace,
        '--backing-store',
        '--additional-data',
        '--clean-output',
      ];

      try {
        const { stdout, stderr } = await execAsync(args.join(' '), { cwd: ROOT });
        const combined = (stdout + stderr).toLowerCase();
        if (combined.includes('error:') || combined.includes('fail:')) {
          throw new Error((stderr || stdout).trim());
        }
        log(`    ✓ ${entry.className} ← ${entry.spec}`, c.green);
      } catch (err) {
        log(`    ✗ ${entry.className} ← ${entry.spec}`, c.red);
        console.error(err.message ?? err);
        process.exit(1);
      }
    }
  }
}

// ── Main ──────────────────────────────────────────────────────────────────────

async function main() {
  const args       = process.argv.slice(2);
  const serviceIdx = args.findIndex(a => a === '--service' || a === '-s');
  const service    = serviceIdx !== -1 ? args[serviceIdx + 1] : null;

  log('Backend API Client Updater', c.cyan);
  log('══════════════════════════', c.cyan);

  if (service && !CONSUMERS[service]) {
    log(
      `Unknown service: "${service}".  Valid options: ${Object.keys(CONSUMERS).join(', ')}`,
      c.red,
    );
    process.exit(1);
  }

  const consumers = service ? { [service]: CONSUMERS[service] } : CONSUMERS;

  await exportSpecs(consumers);
  await generateClients(consumers);

  log('\n✓ Done. Commit any changes in docs/openapi/ along with the regenerated clients.\n', c.green);
}

main().catch(err => { console.error(err); process.exit(1); });
