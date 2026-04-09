/**
 * Global setup: verify that the stack is healthy before running E2E tests.
 * Fails fast if the gateway or frontend is unreachable.
 */
async function globalSetup() {
  const gatewayUrl = process.env.GATEWAY_URL ?? 'http://localhost:8080';
  const frontendUrl = process.env.BASE_URL ?? 'http://localhost:5173';

  const checks = [
    { name: 'API Gateway', url: `${gatewayUrl}/health/ready` },
    { name: 'Frontend', url: frontendUrl },
  ];

  for (const check of checks) {
    try {
      const res = await fetch(check.url, { signal: AbortSignal.timeout(5_000) });
      if (!res.ok) {
        throw new Error(`${check.name} returned HTTP ${res.status}`);
      }
      console.log(`  ✓ ${check.name} is healthy (${check.url})`);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : String(err);
      throw new Error(
        `${check.name} health check failed (${check.url}): ${msg}\n` +
          'Make sure the full dev stack is running (infra + services + frontend).',
      );
    }
  }
}

export default globalSetup;
