import { test as base, expect, type Page } from '@playwright/test';
import { loginViaUI } from './auth';

/**
 * Test credentials — must match a user registered in the system.
 * Override via E2E_USER_EMAIL / E2E_USER_PASSWORD env vars.
 */
export const TEST_USER = {
  email: process.env.E2E_USER_EMAIL ?? 'admin@erp.com',
  password: process.env.E2E_USER_PASSWORD ?? 'Admin123!',
};

type Fixtures = {
  /** A page already authenticated via UI login. */
  authenticatedPage: Page;
};

/**
 * Extended test fixture that provides an authenticated page.
 * Most tests should use the default `test` import from `@playwright/test`
 * with the `storageState` from `playwright.config.ts` instead.
 * Use this fixture when you need fine-grained control.
 */
export const test = base.extend<Fixtures>({
  authenticatedPage: async ({ page }, use) => {
    await loginViaUI(page, TEST_USER.email, TEST_USER.password);
    await use(page);
  },
});

export { expect } from '@playwright/test';
