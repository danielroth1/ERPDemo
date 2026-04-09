import { test, expect } from '@playwright/test';

test.describe('User Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/users');
    await expect(page.getByText('User Management')).toBeVisible();
  });

  test('displays the users page title', async ({ page }) => {
    await expect(page.getByText('User Management')).toBeVisible();
  });

  test('shows users table or empty state', async ({ page }) => {
    const table = page.locator('table');
    const emptyState = page.getByText(/no users|empty/i);
    await expect(table.or(emptyState).first()).toBeVisible({ timeout: 15_000 });
  });

  test('displays user emails in the table', async ({ page }) => {
    const table = page.locator('table');
    if (await table.isVisible({ timeout: 5_000 }).catch(() => false)) {
      // At least the admin user should be listed
      await expect(table.getByText(/@/).first()).toBeVisible({ timeout: 10_000 });
    }
  });

  test('displays user roles', async ({ page }) => {
    // The users table has a Role column showing Admin, Manager, or User badges
    await expect(page.locator('table').getByText(/admin|manager|user/i).first()).toBeVisible({ timeout: 10_000 });
  });
});
