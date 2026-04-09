import { describe, it, expect, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LoginPage } from './LoginPage';
import { renderWithProviders, unauthenticatedState } from '../../test/test-utils';
import { server } from '../../test/mocks/server';
import { http, HttpResponse } from 'msw';
import toast from 'react-hot-toast';

// Mock react-hot-toast
vi.mock('react-hot-toast', () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
  },
}));

describe('LoginPage', () => {
  it('renders login form with email and password fields', () => {
    renderWithProviders(<LoginPage />, {
      preloadedState: unauthenticatedState,
      route: '/login',
    });

    expect(screen.getByText('ERP System')).toBeInTheDocument();
    expect(screen.getByText('Sign in to your account')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Email address')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Password')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument();
  });

  it('has a link to register page', () => {
    renderWithProviders(<LoginPage />, {
      preloadedState: unauthenticatedState,
      route: '/login',
    });

    const registerLink = screen.getByText(/don't have an account/i);
    expect(registerLink).toBeInTheDocument();
    expect(registerLink).toHaveAttribute('href', '/register');
  });

  it('allows typing in form fields', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />, {
      preloadedState: unauthenticatedState,
      route: '/login',
    });

    const emailInput = screen.getByPlaceholderText('Email address');
    const passwordInput = screen.getByPlaceholderText('Password');

    await user.type(emailInput, 'test@example.com');
    await user.type(passwordInput, 'password123');

    expect(emailInput).toHaveValue('test@example.com');
    expect(passwordInput).toHaveValue('password123');
  });

  it('submits login and calls success toast', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginPage />, {
      preloadedState: unauthenticatedState,
      route: '/login',
    });

    await user.type(screen.getByPlaceholderText('Email address'), 'test@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'password123');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(vi.mocked(toast.success)).toHaveBeenCalledWith('Login successful!');
    });
  });

  it('displays error message from Redux state', () => {
    renderWithProviders(<LoginPage />, {
      preloadedState: {
        auth: {
          user: null,
          isAuthenticated: false,
          isLoading: false,
          error: 'Invalid credentials',
        },
      },
      route: '/login',
    });

    expect(screen.getByText('Invalid credentials')).toBeInTheDocument();
  });

  it('disables submit button while loading', () => {
    renderWithProviders(<LoginPage />, {
      preloadedState: {
        auth: {
          user: null,
          isAuthenticated: false,
          isLoading: true,
          error: null,
        },
      },
      route: '/login',
    });

    const button = screen.getByRole('button', { name: /signing in/i });
    expect(button).toBeDisabled();
  });

  it('handles server error on login', async () => {
    server.use(
      http.post('/api/v1/auth/login', () => {
        return HttpResponse.json(
          { message: 'Invalid email or password' },
          { status: 401 }
        );
      })
    );

    const user = userEvent.setup();
    renderWithProviders(<LoginPage />, {
      preloadedState: unauthenticatedState,
      route: '/login',
    });

    await user.type(screen.getByPlaceholderText('Email address'), 'wrong@example.com');
    await user.type(screen.getByPlaceholderText('Password'), 'wrongpassword');
    await user.click(screen.getByRole('button', { name: /sign in/i }));

    await waitFor(() => {
      expect(vi.mocked(toast.error)).toHaveBeenCalled();
    });
  });
});
