import { test, expect } from '@playwright/test';

test.describe('Database Overview', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/database');
  });

  test('displays the database overview page', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /database management/i })).toBeVisible();
  });

  test('shows database tables or schema info', async ({ page }) => {
    // The database page has a 'Service Databases' section heading
    await expect(page.getByRole('heading', { name: /service databases/i })).toBeVisible({ timeout: 15_000 });
  });
});
