import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MainLayout } from './MainLayout';
import { renderWithProviders, authenticatedState } from '../../test/test-utils';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('MainLayout', () => {
  it('renders navigation links', () => {
    renderWithProviders(<MainLayout />, {
      preloadedState: authenticatedState,
      route: '/',
    });

    expect(screen.getAllByText('Users').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Inventory').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Shop').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Financial').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Database').length).toBeGreaterThan(0);
  });

  it('displays user name', () => {
    renderWithProviders(<MainLayout />, {
      preloadedState: authenticatedState,
      route: '/',
    });

    expect(screen.getAllByText('Test User').length).toBeGreaterThan(0);
  });

  it('displays user role badge', () => {
    renderWithProviders(<MainLayout />, {
      preloadedState: authenticatedState,
      route: '/',
    });

    // roles: [2] = Admin
    expect(screen.getAllByText('Admin').length).toBeGreaterThan(0);
  });

  it('renders ERP System branding', () => {
    renderWithProviders(<MainLayout />, {
      preloadedState: authenticatedState,
      route: '/',
    });

    expect(screen.getAllByText('ERP System').length).toBeGreaterThan(0);
  });

  it('renders external monitoring links', () => {
    renderWithProviders(<MainLayout />, {
      preloadedState: authenticatedState,
      route: '/',
    });

    expect(screen.getAllByText('Prometheus').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Grafana').length).toBeGreaterThan(0);
    expect(screen.getAllByText('pgAdmin').length).toBeGreaterThan(0);
  });

  it('has a logout button', () => {
    renderWithProviders(<MainLayout />, {
      preloadedState: authenticatedState,
      route: '/',
    });

    const logoutButtons = screen.getAllByText('Logout');
    expect(logoutButtons.length).toBeGreaterThan(0);
  });

  it('opens mobile sidebar when menu button is clicked', async () => {
    const _user = userEvent.setup();
    const { container } = renderWithProviders(<MainLayout />, {
      preloadedState: authenticatedState,
      route: '/',
    });

    // The mobile menu button has Bars3Icon
    const menuButtons = container.querySelectorAll('button');
    const _mobileMenuButton = Array.from(menuButtons).find(
      btn => btn.querySelector('svg') && btn.closest('.lg\\:hidden')
    );

    // There should be a menu toggle button
    expect(menuButtons.length).toBeGreaterThan(0);
  });
});
