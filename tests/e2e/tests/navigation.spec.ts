import { test, expect } from '@playwright/test';

test.describe('Navigation & Layout', () => {
  test('sidebar shows all navigation links', async ({ page }) => {
    await page.goto('/inventory');

    const nav = page.locator('[data-testid="sidebar"]');
    await expect(nav.getByRole('link', { name: /inventory/i })).toBeVisible();
    await expect(nav.getByRole('link', { name: /users/i })).toBeVisible();
    await expect(nav.getByRole('link', { name: /sales/i })).toBeVisible();
    await expect(nav.getByRole('link', { name: /financial/i })).toBeVisible();
    await expect(nav.getByRole('link', { name: /analytics/i })).toBeVisible();
    await expect(nav.getByRole('link', { name: /shop/i })).toBeVisible();
  });

  test('navigates to inventory page', async ({ page }) => {
    await page.goto('/');
    // Default redirect should go to /inventory
    await expect(page).toHaveURL(/\/inventory/);
    await expect(page.getByText('Inventory Management')).toBeVisible();
  });

  test('navigates to users page', async ({ page }) => {
    await page.goto('/users');
    await expect(page).toHaveURL(/\/users/);
    await expect(page.getByText('User Management')).toBeVisible();
  });

  test('navigates to sales page', async ({ page }) => {
    await page.goto('/sales');
    await expect(page).toHaveURL(/\/sales/);
    await expect(page.getByText('Sales & Orders')).toBeVisible();
  });

  test('navigates to financial page', async ({ page }) => {
    await page.goto('/financial');
    await expect(page).toHaveURL(/\/financial/);
    await expect(page.getByText('Financial Management')).toBeVisible();
  });

  test('navigates to analytics page', async ({ page }) => {
    await page.goto('/analytics');
    await expect(page).toHaveURL(/\/analytics/);
    await expect(page.getByRole('heading', { name: /analytics/i })).toBeVisible();
  });

  test('navigates to shop page', async ({ page }) => {
    await page.goto('/shop');
    await expect(page).toHaveURL(/\/shop/);
  });

  test('displays current user info in sidebar', async ({ page }) => {
    await page.goto('/inventory');
    // The sidebar footer shows the logged-in user's name
    await expect(page.getByText('Admin User')).toBeVisible();
  });
});
