export function getApiBaseUrl(): string {
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
