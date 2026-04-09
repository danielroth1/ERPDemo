import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders, authenticatedState } from '../../test/test-utils';
import { UsersPage } from './UsersPage';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn().mockImplementation((promise) => promise),
  },
}));

// Mock user service
vi.mock('../../services/user.service', () => ({
  userService: {
    getUsers: vi.fn().mockResolvedValue({
      items: [
        {
          id: 'user-1',
          email: 'admin@example.com',
          firstName: 'Admin',
          lastName: 'User',
          roles: [2],
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 'user-2',
          email: 'manager@example.com',
          firstName: 'Manager',
          lastName: 'User',
          roles: [1],
          isActive: true,
          createdAt: '2024-01-02T00:00:00Z',
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 2,
      totalPages: 1,
    }),
    updateUser: vi.fn().mockResolvedValue({}),
    deleteUser: vi.fn().mockResolvedValue(undefined),
    activateUser: vi.fn().mockResolvedValue({}),
    deactivateUser: vi.fn().mockResolvedValue({}),
  },
}));

// Mock financial service (called directly)
vi.mock('../../services/financial.service', () => ({
  financialService: {
    getUserAccounts: vi.fn().mockResolvedValue([]),
    createUserAccount: vi.fn().mockResolvedValue({}),
    deleteUserAccount: vi.fn().mockResolvedValue(undefined),
  },
}));

describe('UsersPage', () => {
  it('renders page title', async () => {
    renderWithProviders(<UsersPage />, {
      preloadedState: authenticatedState,
      route: '/users',
    });

    await waitFor(() => {
      expect(screen.getByText('User Management')).toBeInTheDocument();
    });
  });

  it('loads and displays users from API', async () => {
    renderWithProviders(<UsersPage />, {
      preloadedState: authenticatedState,
      route: '/users',
    });

    await waitFor(() => {
      expect(screen.getByText('admin@example.com')).toBeInTheDocument();
      expect(screen.getByText('manager@example.com')).toBeInTheDocument();
    });
  });

  it('displays user names', async () => {
    renderWithProviders(<UsersPage />, {
      preloadedState: authenticatedState,
      route: '/users',
    });

    await waitFor(() => {
      expect(screen.getByText('Admin User')).toBeInTheDocument();
      expect(screen.getByText('Manager User')).toBeInTheDocument();
    });
  });

  it('handles empty user list', async () => {
    const { userService } = await import('../../services/user.service');
    vi.mocked(userService.getUsers).mockResolvedValueOnce({
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    });

    renderWithProviders(<UsersPage />, {
      preloadedState: authenticatedState,
      route: '/users',
    });

    await waitFor(() => {
      expect(screen.getByText('User Management')).toBeInTheDocument();
    });
  });

  it('handles API error gracefully', async () => {
    const { userService } = await import('../../services/user.service');
    vi.mocked(userService.getUsers).mockRejectedValueOnce(
      new Error('Server error')
    );

    renderWithProviders(<UsersPage />, {
      preloadedState: authenticatedState,
      route: '/users',
    });

    await waitFor(() => {
      expect(screen.getByText('User Management')).toBeInTheDocument();
    });
  });
});
