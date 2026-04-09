import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders, authenticatedState } from '../../test/test-utils';
import { ShopPage } from './ShopPage';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn().mockImplementation((promise) => promise),
  },
}));

// Mock shop service
vi.mock('../../services/shop.service', () => ({
  shopService: {
    getAvailableProducts: vi.fn().mockResolvedValue([
      {
        id: 'prod-1',
        name: 'Widget A',
        description: 'A test widget',
        sku: 'WGT-001',
        price: 29.99,
        stockQuantity: 100,
        categoryId: 'cat-1',
        categoryName: 'Electronics',
        imageUrl: null,
      },
    ]),
    getCategories: vi.fn().mockResolvedValue([
      { id: 'cat-1', name: 'Electronics' },
    ]),
    purchaseProducts: vi.fn().mockResolvedValue({ orderId: 'order-1', status: 'Pending' }),
  },
}));

// Mock financial service
vi.mock('../../services/financial.service', () => ({
  financialService: {
    getUserAccounts: vi.fn().mockResolvedValue([
      { id: 'acc-1', accountNumber: 'ACC-001', name: 'Cash', balance: 5000, currency: 'USD' },
    ]),
  },
}));

describe('ShopPage', () => {
  it('renders the shop page', async () => {
    renderWithProviders(<ShopPage />, {
      preloadedState: authenticatedState,
      route: '/shop',
    });

    await waitFor(() => {
      expect(screen.getByText('Shop')).toBeInTheDocument();
    });
  });

  it('loads and displays products', async () => {
    renderWithProviders(<ShopPage />, {
      preloadedState: authenticatedState,
      route: '/shop',
    });

    await waitFor(() => {
      expect(screen.getByText('Widget A')).toBeInTheDocument();
    });
  });

  it('loads categories', async () => {
    renderWithProviders(<ShopPage />, {
      preloadedState: authenticatedState,
      route: '/shop',
    });

    await waitFor(() => {
      expect(screen.getByText('Electronics')).toBeInTheDocument();
    });
  });
});
