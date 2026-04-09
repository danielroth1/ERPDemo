import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders, authenticatedState } from '../../test/test-utils';
import { SalesPage } from './SalesPage';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn().mockImplementation((promise) => promise),
  },
}));

// Mock sales service
vi.mock('../../services/sales.service', () => ({
  salesService: {
    getOrders: vi.fn().mockResolvedValue({
      items: [
        {
          id: 'order-1',
          orderNumber: 'ORD-001',
          customerId: 'cust-1',
          orderDate: '2024-01-15T00:00:00Z',
          status: 'Pending',
          totalAmount: 299.99,
          notes: '',
          items: [],
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    }),
    getCustomers: vi.fn().mockResolvedValue([
      { id: 'cust-1', name: 'Acme Corp', email: 'contact@acme.com', phone: '555-0100', isActive: true },
    ]),
    createOrder: vi.fn().mockResolvedValue({ id: 'new-order' }),
    updateOrderStatus: vi.fn().mockResolvedValue({}),
    deleteOrder: vi.fn().mockResolvedValue(undefined),
  },
}));

describe('SalesPage', () => {
  it('renders page title', async () => {
    renderWithProviders(<SalesPage />, {
      preloadedState: authenticatedState,
      route: '/sales',
    });

    await waitFor(() => {
      expect(screen.getByText('Sales & Orders')).toBeInTheDocument();
    });
  });

  it('loads and displays orders from API', async () => {
    renderWithProviders(<SalesPage />, {
      preloadedState: authenticatedState,
      route: '/sales',
    });

    await waitFor(() => {
      expect(screen.getByText(/ORD-001/)).toBeInTheDocument();
    });
  });

  it('handles empty orders list', async () => {
    const { salesService } = await import('../../services/sales.service');
    vi.mocked(salesService.getOrders).mockResolvedValueOnce({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    });

    renderWithProviders(<SalesPage />, {
      preloadedState: authenticatedState,
      route: '/sales',
    });

    await waitFor(() => {
      expect(screen.getByText('Sales & Orders')).toBeInTheDocument();
    });
  });
});
