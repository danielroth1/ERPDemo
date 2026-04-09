import { test, expect } from '@playwright/test';

test.describe('Financial Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/financial');
    await expect(page.getByText('Financial Management')).toBeVisible();
  });

  test('displays the financial page title', async ({ page }) => {
    await expect(page.getByText('Financial Management')).toBeVisible();
  });

  test('shows balance summary cards', async ({ page }) => {
    await expect(page.getByText(/total assets/i)).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText(/total liabilities/i)).toBeVisible();
    await expect(page.getByText(/equity/i)).toBeVisible();
  });

  test('shows transactions table or empty state', async ({ page }) => {
    const table = page.locator('table');
    const emptyState = page.getByText(/no transactions/i);
    await expect(table.or(emptyState).first()).toBeVisible({ timeout: 15_000 });
  });

  test('transactions table has correct headers', async ({ page }) => {
    const table = page.locator('table');
    if (await table.isVisible({ timeout: 5_000 }).catch(() => false)) {
      await expect(table.getByText(/date/i).first()).toBeVisible();
      await expect(table.getByText(/reference/i).first()).toBeVisible();
      await expect(table.getByText(/description/i).first()).toBeVisible();
      await expect(table.getByText(/amount/i).first()).toBeVisible();
    }
  });
});
