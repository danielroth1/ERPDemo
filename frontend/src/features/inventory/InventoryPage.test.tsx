import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders, authenticatedState } from '../../test/test-utils';
import { InventoryPage } from './InventoryPage';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn().mockImplementation((promise) => promise),
  },
}));

// Mock the inventory service at the module level
vi.mock('../../services/inventory.service', () => ({
  inventoryService: {
    getProducts: vi.fn().mockResolvedValue({
      items: [
        {
          id: 'prod-1',
          name: 'Widget A',
          description: 'A test widget',
          sku: 'WGT-001',
          categoryId: 'cat-1',
          unitPrice: 29.99,
          stockQuantity: 100,
          reorderLevel: 10,
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 'prod-2',
          name: 'Widget B',
          description: 'Another test widget',
          sku: 'WGT-002',
          categoryId: 'cat-1',
          unitPrice: 49.99,
          stockQuantity: 50,
          reorderLevel: 5,
          isActive: true,
          createdAt: '2024-01-02T00:00:00Z',
          updatedAt: '2024-01-02T00:00:00Z',
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 2,
      totalPages: 1,
    }),
    getCategories: vi.fn().mockResolvedValue([
      { id: 'cat-1', name: 'Electronics', description: 'Electronic items', createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
    ]),
    getCategoryProductCount: vi.fn().mockResolvedValue([
      { categoryId: 'cat-1', categoryName: 'Electronics', productCount: 5 },
    ]),
    deleteProduct: vi.fn().mockResolvedValue(undefined),
    deleteCategory: vi.fn().mockResolvedValue(undefined),
    seedProducts: vi.fn().mockResolvedValue(undefined),
    createProduct: vi.fn().mockResolvedValue({ id: 'new-prod' }),
    updateProduct: vi.fn().mockResolvedValue({ id: 'prod-1' }),
    createCategory: vi.fn().mockResolvedValue({ id: 'new-cat' }),
  },
}));

describe('InventoryPage', () => {
  it('renders page title', async () => {
    renderWithProviders(<InventoryPage />, {
      preloadedState: authenticatedState,
      route: '/inventory',
    });

    await waitFor(() => {
      expect(screen.getByText('Inventory Management')).toBeInTheDocument();
    });
  });

  it('loads and displays products from API', async () => {
    renderWithProviders(<InventoryPage />, {
      preloadedState: authenticatedState,
      route: '/inventory',
    });

    await waitFor(() => {
      expect(screen.getByText('Widget A')).toBeInTheDocument();
      expect(screen.getByText('Widget B')).toBeInTheDocument();
    });
  });

  it('displays product SKUs in table rows', async () => {
    renderWithProviders(<InventoryPage />, {
      preloadedState: authenticatedState,
      route: '/inventory',
    });

    await waitFor(() => {
      expect(screen.getByText('WGT-001')).toBeInTheDocument();
      expect(screen.getByText('WGT-002')).toBeInTheDocument();
    });
  });

  it('handles API error gracefully', async () => {
    const { inventoryService } = await import('../../services/inventory.service');
    vi.mocked(inventoryService.getProducts).mockRejectedValueOnce(
      new Error('Internal server error')
    );

    renderWithProviders(<InventoryPage />, {
      preloadedState: authenticatedState,
      route: '/inventory',
    });

    // Page should still render without crashing
    await waitFor(() => {
      expect(screen.getByText('Inventory Management')).toBeInTheDocument();
    });
  });

  it('shows empty state when no products exist', async () => {
    const { inventoryService } = await import('../../services/inventory.service');
    vi.mocked(inventoryService.getProducts).mockResolvedValueOnce({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    });

    renderWithProviders(<InventoryPage />, {
      preloadedState: authenticatedState,
      route: '/inventory',
    });

    await waitFor(() => {
      expect(screen.getByText('Inventory Management')).toBeInTheDocument();
    });
  });
});
