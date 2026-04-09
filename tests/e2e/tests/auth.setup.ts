import { test as setup, expect } from '@playwright/test';
import { loginViaUI } from '../helpers/auth';
import { TEST_USER } from '../helpers/fixtures';

const authFile = 'playwright/.auth/user.json';

setup('authenticate', async ({ page }) => {
  await loginViaUI(page, TEST_USER.email, TEST_USER.password);

  // Confirm we reached a protected page (dashboard, etc.)
  await expect(page.getByRole('heading', { name: /dashboard/i })).toBeVisible({ timeout: 10_000 });

  // Persist browser storage so subsequent tests skip the login step
  await page.context().storageState({ path: authFile });
});
