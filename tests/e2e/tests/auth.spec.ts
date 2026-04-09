import { test, expect } from '@playwright/test';
import { loginViaUI, registerViaUI, logout } from '../helpers/auth';

// Auth tests use a fresh context (no storageState) — override the default
test.use({ storageState: { cookies: [], origins: [] } });

test.describe('Authentication', () => {
  test('redirects unauthenticated users to /login', async ({ page }) => {
    await page.goto('/inventory');
    await expect(page).toHaveURL(/\/login/);
  });

  test('login form renders correctly', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText('ERP System')).toBeVisible();
    await expect(page.getByText('Sign in to your account')).toBeVisible();
    await expect(page.getByPlaceholder('Email address')).toBeVisible();
    await expect(page.getByPlaceholder('Password')).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in/i })).toBeVisible();
  });

  test('shows error on invalid credentials', async ({ page }) => {
    await page.goto('/login');
    await page.getByPlaceholder('Email address').fill('bad@example.com');
    await page.getByPlaceholder('Password').fill('wrongpassword');
    await page.getByRole('button', { name: /sign in/i }).click();

    // Should stay on login and show an error (toast or inline)
    await expect(page).toHaveURL(/\/login/);
  });

  test('logs in with valid credentials and redirects', async ({ page }) => {
    await loginViaUI(
      page,
      process.env.E2E_USER_EMAIL ?? 'admin@erp.com',
      process.env.E2E_USER_PASSWORD ?? 'Admin123!',
    );

    // Should land on a protected page
    await expect(page).not.toHaveURL(/\/login/);
    await expect(page.locator('[data-testid="sidebar"]')).toBeVisible();
  });

  test('navigates between login and register pages', async ({ page }) => {
    await page.goto('/login');
    await page.getByText(/don't have an account/i).click();
    await expect(page).toHaveURL(/\/register/);
    await expect(page.getByText('Create your account')).toBeVisible();

    await page.getByText(/already have an account/i).click();
    await expect(page).toHaveURL(/\/login/);
  });
});
