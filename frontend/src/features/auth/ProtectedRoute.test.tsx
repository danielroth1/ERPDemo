import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { ProtectedRoute } from './ProtectedRoute';
import { renderWithProviders, authenticatedState, unauthenticatedState } from '../../test/test-utils';

describe('ProtectedRoute', () => {
  it('renders children when authenticated', () => {
    renderWithProviders(
      <ProtectedRoute>
        <div>Protected content</div>
      </ProtectedRoute>,
      { preloadedState: authenticatedState }
    );
    expect(screen.getByText('Protected content')).toBeInTheDocument();
  });

  it('redirects to /login when not authenticated', () => {
    renderWithProviders(
      <ProtectedRoute>
        <div>Protected content</div>
      </ProtectedRoute>,
      {
        preloadedState: unauthenticatedState,
        route: '/dashboard',
      }
    );
    expect(screen.queryByText('Protected content')).not.toBeInTheDocument();
  });
});
