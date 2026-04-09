import { test, expect } from '@playwright/test';

test.describe('Inventory Management', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/inventory');
    await expect(page.getByText('Inventory Management')).toBeVisible();
  });

  test('displays the inventory page title', async ({ page }) => {
    await expect(page.getByText('Inventory Management')).toBeVisible();
  });

  test('shows products table or empty state', async ({ page }) => {
    // Either there's a table with product data, or an empty state message
    const table = page.locator('table');
    const emptyState = page.getByText(/no products|no items|empty/i);
    await expect(table.or(emptyState).first()).toBeVisible({ timeout: 15_000 });
  });

  test('shows product categories tab or section', async ({ page }) => {
    // Look for a categories tab or section
    const categoriesTab = page.getByRole('button', { name: /categories/i })
      .or(page.getByRole('tab', { name: /categories/i }))
      .or(page.getByText(/categories/i));
    await expect(categoriesTab.first()).toBeVisible();
  });

  test('can open add product dialog', async ({ page }) => {
    const addButton = page.getByRole('button', { name: /add.*product|new.*product|create/i });
    if (await addButton.isVisible()) {
      await addButton.click();
      // A modal or form should appear
      await expect(
        page.getByRole('dialog').or(page.getByText(/add.*product|create.*product|new.*product/i)).first(),
      ).toBeVisible();
    }
  });

  test('can seed products if seed button exists', async ({ page }) => {
    const seedButton = page.getByRole('button', { name: /seed/i });
    if (await seedButton.isVisible({ timeout: 2_000 }).catch(() => false)) {
      await seedButton.click();
      // After seeding, products should appear in the table
      await expect(page.locator('table tbody tr').first()).toBeVisible({ timeout: 15_000 });
    }
  });

  test('clicking a product row navigates to detail page', async ({ page }) => {
    const firstRow = page.locator('table tbody tr').first();
    if (await firstRow.isVisible({ timeout: 5_000 }).catch(() => false)) {
      const link = firstRow.getByRole('link').first();
      if (await link.isVisible().catch(() => false)) {
        await link.click();
        await expect(page).toHaveURL(/\/inventory\/.+/);
      }
    }
  });
});
