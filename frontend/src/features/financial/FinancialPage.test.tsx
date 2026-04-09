import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders, authenticatedState } from '../../test/test-utils';
import { FinancialPage } from './FinancialPage';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn().mockImplementation((promise) => promise),
  },
}));

// Mock financial service (used by Redux thunks for accounts & transactions)
vi.mock('../../services/financial.service', () => ({
  financialService: {
    getAccounts: vi.fn().mockResolvedValue([
      { id: 'acc-1', name: 'Cash', accountNumber: '1000', type: 'Asset', balance: 50000, isActive: true, createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
      { id: 'acc-2', name: 'Revenue', accountNumber: '4000', type: 'Revenue', balance: 75000, isActive: true, createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
    ]),
    getTransactions: vi.fn().mockResolvedValue({
      items: [
        { id: 'txn-1', transactionNumber: 'TXN-001', date: '2024-01-15T00:00:00Z', description: 'Office supplies', amount: 150.00, type: 'Purchase', debitAccountId: 'acc-1', creditAccountId: 'acc-2', reference: 'REF-001', createdAt: '2024-01-15T00:00:00Z' },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    }),
    deleteTransaction: vi.fn().mockResolvedValue(undefined),
    createTransaction: vi.fn().mockResolvedValue({ id: 'new-txn' }),
  },
}));

// Mock financial-api.client (called directly for balance summary)
vi.mock('../../services/financial-api.client', () => ({
  financialApiClient: {
    getAccountBalanceSummary: vi.fn().mockResolvedValue({
      totalAssets: 100000,
      totalLiabilities: 30000,
      totalEquity: 70000,
    }),
  },
}));

describe('FinancialPage', () => {
  it('renders page title and balance summary', async () => {
    renderWithProviders(<FinancialPage />, {
      preloadedState: authenticatedState,
      route: '/financial',
    });

    await waitFor(() => {
      expect(screen.getByText('Financial Management')).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText('$100000.00')).toBeInTheDocument();
      expect(screen.getByText('$30000.00')).toBeInTheDocument();
      expect(screen.getByText('$70000.00')).toBeInTheDocument();
    });
  });

  it('loads and displays transactions from API', async () => {
    renderWithProviders(<FinancialPage />, {
      preloadedState: authenticatedState,
      route: '/financial',
    });

    await waitFor(() => {
      expect(screen.getByText('Office supplies')).toBeInTheDocument();
      expect(screen.getByText('REF-001')).toBeInTheDocument();
    });
  });

  it('handles API error gracefully', async () => {
    const { financialService } = await import('../../services/financial.service');
    vi.mocked(financialService.getAccounts).mockRejectedValueOnce(
      new Error('Server error')
    );

    renderWithProviders(<FinancialPage />, {
      preloadedState: authenticatedState,
      route: '/financial',
    });

    await waitFor(() => {
      expect(screen.getByText('Financial Management')).toBeInTheDocument();
    });
  });
});
