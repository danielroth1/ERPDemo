import { test, expect } from '@playwright/test';

test.describe('Analytics', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/analytics');
  });

  test('displays the analytics page', async ({ page }) => {
    await expect(page.getByRole('heading', { name: /analytics dashboard/i })).toBeVisible();
  });

  test('shows KPI cards', async ({ page }) => {
    // Analytics page should display key performance indicators
    const kpiSection = page.locator('.card, [data-testid="kpi"]').first();
    await expect(kpiSection).toBeVisible({ timeout: 15_000 });
  });

  test('shows revenue or sales metrics', async ({ page }) => {
    // Analytics page has a Total Revenue heading in the KPI section
    await expect(page.getByRole('heading', { name: /total revenue/i })).toBeVisible({ timeout: 15_000 });
  });
});
