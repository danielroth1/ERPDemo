import { type Page, expect } from '@playwright/test';

/**
 * Log in via the UI by filling the login form.
 */
export async function loginViaUI(
  page: Page,
  email: string,
  password: string,
): Promise<void> {
  await page.goto('/login');
  await page.getByPlaceholder('Email address').fill(email);
  await page.getByPlaceholder('Password').fill(password);
  await page.getByRole('button', { name: /sign in/i }).click();
  // Wait for navigation away from login page
  await expect(page).not.toHaveURL(/\/login/);
}

/**
 * Log out via the sidebar button.
 */
export async function logout(page: Page): Promise<void> {
  await page.getByRole('button', { name: /logout/i }).click();
  await expect(page).toHaveURL(/\/login/);
}

/**
 * Register a new user via the UI.
 */
export async function registerViaUI(
  page: Page,
  data: { firstName: string; lastName: string; email: string; password: string },
): Promise<void> {
  await page.goto('/register');
  await page.getByLabel('First Name').fill(data.firstName);
  await page.getByLabel('Last Name').fill(data.lastName);
  await page.getByLabel('Email address').fill(data.email);
  await page.getByLabel('Password', { exact: true }).fill(data.password);
  await page.getByLabel('Confirm Password').fill(data.password);
  await page.getByRole('button', { name: /register/i }).click();
  await expect(page).not.toHaveURL(/\/register/);
}
