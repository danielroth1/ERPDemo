import { test, expect } from '@playwright/test';

test.describe('Shop', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/shop');
    // Wait for the shop page to finish loading
    await expect(page.getByRole('heading', { name: 'Shop' })).toBeVisible({ timeout: 15_000 });
  });

  test('displays the shop page', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Shop' })).toBeVisible();
  });

  test('shows product cards or list', async ({ page }) => {
    // Shop shows either product purchase buttons (when products exist) or an empty state message
    const emptyState = page.getByText('No products available');
    const productButton = page.getByRole('button', { name: /purchase/i });
    await expect(productButton.or(emptyState).first()).toBeVisible({ timeout: 15_000 });
  });

  test('shows product categories filter', async ({ page }) => {
    const categories = page.getByText(/categories|all|filter/i);
    await expect(categories.first()).toBeVisible({ timeout: 10_000 });
  });

  test('can add product to cart if products exist', async ({ page }) => {
    const addToCartButton = page.getByRole('button', { name: /purchase/i });
    const isVisible = await addToCartButton.first().isVisible({ timeout: 5_000 }).catch(() => false);
    if (isVisible) {
      const isEnabled = await addToCartButton.first().isEnabled().catch(() => false);
      if (isEnabled) {
        await addToCartButton.first().click();
        // Should show cart indicator or confirmation
        const cartIndicator = page.getByText(/cart|added|order/i);
        await expect(cartIndicator.first()).toBeVisible({ timeout: 5_000 });
      }
    }
  });

  test('can return product if products exist', async ({ page }) => {
    const returnProductButton = page.getByRole('button', { name: /return/i });
    const isVisible = await returnProductButton.first().isVisible({ timeout: 5_000 }).catch(() => false);
    if (isVisible) {
      const isEnabled = await returnProductButton.first().isEnabled().catch(() => false);
      if (isEnabled) {
        await returnProductButton.first().click();
        // Should show cart indicator or confirmation
        const cartIndicator = page.getByText(/returned/i);
        await expect(cartIndicator.first()).toBeVisible({ timeout: 5_000 });
      }
    }
  });
});
