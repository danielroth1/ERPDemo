import type { ReactElement, ReactNode } from 'react';
/* eslint-disable react-refresh/only-export-components */
import { render, type RenderOptions } from '@testing-library/react';
import { Provider } from 'react-redux';
import { BrowserRouter, MemoryRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import authReducer from '../features/auth/authSlice';
import inventoryReducer from '../features/inventory/inventorySlice';
import usersReducer from '../features/users/usersSlice';
import salesReducer from '../features/sales/salesSlice';
import financialReducer from '../features/financial/financialSlice';
import analyticsReducer from '../features/analytics/analyticsSlice';
import type { RootState } from '../store';

type DeepPartial<T> = {
  [P in keyof T]?: T[P] extends object ? DeepPartial<T[P]> : T[P];
};

interface ExtendedRenderOptions extends Omit<RenderOptions, 'queries'> {
  preloadedState?: DeepPartial<RootState>;
  route?: string;
  useMemoryRouter?: boolean;
}

function createTestStore(preloadedState?: DeepPartial<RootState>) {
  return configureStore({
    reducer: {
      auth: authReducer,
      inventory: inventoryReducer,
      users: usersReducer,
      sales: salesReducer,
      financial: financialReducer,
      analytics: analyticsReducer,
    },
    preloadedState: preloadedState as RootState,
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware({
        serializableCheck: {
          ignoredActions: ['auth/login/fulfilled', 'auth/register/fulfilled'],
        },
      }),
  });
}

function AllProviders({
  children,
  store,
  route,
  useMemoryRouter,
}: {
  children: ReactNode;
  store: ReturnType<typeof createTestStore>;
  route?: string;
  useMemoryRouter?: boolean;
}) {
  const Router = useMemoryRouter ? MemoryRouter : BrowserRouter;
  const routerProps = useMemoryRouter && route ? { initialEntries: [route] } : {};

  return (
    <Provider store={store}>
      <Router {...routerProps}>
        {children}
      </Router>
    </Provider>
  );
}

export function renderWithProviders(
  ui: ReactElement,
  {
    preloadedState,
    route = '/',
    useMemoryRouter = true,
    ...renderOptions
  }: ExtendedRenderOptions = {}
) {
  const store = createTestStore(preloadedState);

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <AllProviders store={store} route={route} useMemoryRouter={useMemoryRouter}>
        {children}
      </AllProviders>
    );
  }

  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) };
}

// Authenticated state factory
export const authenticatedState: DeepPartial<RootState> = {
  auth: {
    user: {
      id: 'user-1',
      email: 'test@example.com',
      firstName: 'Test',
      lastName: 'User',
      roles: [2],
      isActive: true,
      createdAt: '2024-01-01T00:00:00Z',
    },
    isAuthenticated: true,
    isLoading: false,
    error: null,
  },
};

export const unauthenticatedState: DeepPartial<RootState> = {
  auth: {
    user: null,
    isAuthenticated: false,
    isLoading: false,
    error: null,
  },
};

export { createTestStore };
