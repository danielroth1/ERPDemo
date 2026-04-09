import { type Page, expect } from '@playwright/test';

const GATEWAY_URL = process.env.GATEWAY_URL ?? 'http://localhost:8080';

/**
 * Seed data via the API gateway — calls the gateway directly, bypassing the frontend.
 */
export async function apiRequest(
  page: Page,
  method: string,
  path: string,
  body?: unknown,
): Promise<unknown> {
  const token = await page.evaluate(() => localStorage.getItem('accessToken'));
  const res = await page.request.fetch(`${GATEWAY_URL}${path}`, {
    method,
    data: body,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });
  if (!res.ok()) {
    throw new Error(`API ${method} ${path} returned ${res.status()}: ${await res.text()}`);
  }
  return res.json().catch(() => null);
}

/**
 * Register a new user via the API and return the response.
 */
export async function registerUser(
  page: Page,
  user: { firstName: string; lastName: string; email: string; password: string },
): Promise<{ accessToken: string; refreshToken: string; user: Record<string, unknown> }> {
  const res = await page.request.fetch(`${GATEWAY_URL}/api/v1/auth/register`, {
    method: 'POST',
    data: user,
    headers: { 'Content-Type': 'application/json' },
  });
  const json = (await res.json()) as { data: { accessToken: string; refreshToken: string; user: Record<string, unknown> } };
  return json.data;
}

/**
 * Login via the API and return tokens.
 */
export async function loginViaApi(
  email: string,
  password: string,
): Promise<{ accessToken: string; refreshToken: string; user: Record<string, unknown> }> {
  const res = await fetch(`${GATEWAY_URL}/api/v1/auth/login`, {
    method: 'POST',
    body: JSON.stringify({ email, password }),
    headers: { 'Content-Type': 'application/json' },
  });
  if (!res.ok) throw new Error(`Login failed: ${res.status}`);
  const json = (await res.json()) as { data: { accessToken: string; refreshToken: string; user: Record<string, unknown> } };
  return json.data;
}
