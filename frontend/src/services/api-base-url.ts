export function getApiBaseUrl(): string {
  // In dev, we use the Vite proxy (see vite.config.ts) to avoid CORS.
  // Keep requests same-origin by using a relative base URL.
  if (import.meta.env.DEV) {
    return '';
  }

  const configured = (import.meta.env.VITE_API_GATEWAY_URL ?? '').trim();
  if (configured.length > 0) {
    return configured.replace(/\/+$/, '');
  }

  // In production builds (e.g., the nginx container), route through the same origin.
  // This works with our reverse-proxy setup for `/api/*`.
  if (import.meta.env.PROD) {
    return window.location.origin;
  }

  // Local development fallback (VS Code tasks / dotnet watch): gateway default.
  return 'http://localhost:5001';
}
