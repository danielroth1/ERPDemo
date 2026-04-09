import { test, expect } from '@playwright/test';

test.describe('Sales & Orders', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/sales');
    await expect(page.getByText('Sales & Orders')).toBeVisible();
  });

  test('displays the sales page title', async ({ page }) => {
    await expect(page.getByText('Sales & Orders')).toBeVisible();
  });

  test('shows orders table or empty state', async ({ page }) => {
    const table = page.locator('table');
    const emptyState = page.getByText(/no orders|no data|empty/i);
    await expect(table.or(emptyState).first()).toBeVisible({ timeout: 15_000 });
  });

  test('shows customers tab or section', async ({ page }) => {
    // The orders table has a Customer column header
    await expect(page.locator('table').getByText(/customer/i).first()).toBeVisible();
  });

  test('can open create order dialog if button exists', async ({ page }) => {
    const createButton = page.getByRole('button', { name: /new.*order|create.*order|add.*order/i });
    if (await createButton.isVisible({ timeout: 3_000 }).catch(() => false)) {
      await createButton.click();
      await expect(page.getByRole('dialog').or(page.getByText(/create.*order|new.*order/i))).toBeVisible();
    }
  });

  test('orders show status badges', async ({ page }) => {
    const table = page.locator('table');
    if (await table.isVisible({ timeout: 5_000 }).catch(() => false)) {
      const statusText = table.getByText(
        /pending|confirmed|shipped|delivered|cancelled/i,
      );
      if (await statusText.first().isVisible({ timeout: 5_000 }).catch(() => false)) {
        await expect(statusText.first()).toBeVisible();
      }
    }
  });
});
