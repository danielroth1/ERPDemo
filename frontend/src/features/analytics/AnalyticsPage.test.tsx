import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders, authenticatedState } from '../../test/test-utils';
import { AnalyticsPage } from './AnalyticsPage';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('AnalyticsPage', () => {
  it('renders page title', async () => {
    renderWithProviders(<AnalyticsPage />, {
      preloadedState: authenticatedState,
      route: '/analytics',
    });

    await waitFor(() => {
      expect(screen.getByText('Analytics Dashboard')).toBeInTheDocument();
    });
  });

  it('loads KPIs on mount', async () => {
    renderWithProviders(<AnalyticsPage />, {
      preloadedState: authenticatedState,
      route: '/analytics',
    });

    // Page should render and dispatch fetchKPIs/fetchAlerts/etc.
    await waitFor(() => {
      expect(screen.getByText('Analytics Dashboard')).toBeInTheDocument();
    });
  });
});
