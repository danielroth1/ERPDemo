import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RegisterPage } from './RegisterPage';
import { renderWithProviders, unauthenticatedState } from '../../test/test-utils';
import toast from 'react-hot-toast';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('RegisterPage', () => {
  it('renders registration form with all fields', () => {
    renderWithProviders(<RegisterPage />, {
      preloadedState: unauthenticatedState,
      route: '/register',
    });

    expect(screen.getByText('Create your account')).toBeInTheDocument();
    expect(screen.getByLabelText('First Name')).toBeInTheDocument();
    expect(screen.getByLabelText('Last Name')).toBeInTheDocument();
    expect(screen.getByLabelText('Email address')).toBeInTheDocument();
    expect(screen.getByLabelText('Password')).toBeInTheDocument();
    expect(screen.getByLabelText('Confirm Password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /register/i })).toBeInTheDocument();
  });

  it('has a link to login page', () => {
    renderWithProviders(<RegisterPage />, {
      preloadedState: unauthenticatedState,
      route: '/register',
    });

    const loginLink = screen.getByText(/already have an account/i);
    expect(loginLink).toBeInTheDocument();
    expect(loginLink).toHaveAttribute('href', '/login');
  });

  it('shows error toast when passwords do not match', async () => {
    const user = userEvent.setup();

    renderWithProviders(<RegisterPage />, {
      preloadedState: unauthenticatedState,
      route: '/register',
    });

    await user.type(screen.getByLabelText('First Name'), 'John');
    await user.type(screen.getByLabelText('Last Name'), 'Doe');
    await user.type(screen.getByLabelText('Email address'), 'john@example.com');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.type(screen.getByLabelText('Confirm Password'), 'differentpass');
    await user.click(screen.getByRole('button', { name: /register/i }));

    expect(vi.mocked(toast.error)).toHaveBeenCalledWith('Passwords do not match');
  });

  it('submits form when passwords match', async () => {
    const user = userEvent.setup();
    renderWithProviders(<RegisterPage />, {
      preloadedState: unauthenticatedState,
      route: '/register',
    });

    await user.type(screen.getByLabelText('First Name'), 'John');
    await user.type(screen.getByLabelText('Last Name'), 'Doe');
    await user.type(screen.getByLabelText('Email address'), 'john@example.com');
    await user.type(screen.getByLabelText('Password'), 'password123');
    await user.type(screen.getByLabelText('Confirm Password'), 'password123');
    await user.click(screen.getByRole('button', { name: /register/i }));

    await waitFor(() => {
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith('Registration successful!');
    });
  });

  it('disables submit button while loading', () => {
    renderWithProviders(<RegisterPage />, {
      preloadedState: {
        auth: {
          user: null,
          isAuthenticated: false,
          isLoading: true,
          error: null,
        },
      },
      route: '/register',
    });

    const button = screen.getByRole('button', { name: /creating account/i });
    expect(button).toBeDisabled();
  });

  it('displays error message from Redux state', () => {
    renderWithProviders(<RegisterPage />, {
      preloadedState: {
        auth: {
          user: null,
          isAuthenticated: false,
          isLoading: false,
          error: 'Email already exists',
        },
      },
      route: '/register',
    });

    expect(screen.getByText('Email already exists')).toBeInTheDocument();
  });
});
