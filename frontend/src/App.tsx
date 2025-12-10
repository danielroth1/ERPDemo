import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Provider } from 'react-redux';
import { ApolloProvider } from '@apollo/client/react';
import { Toaster } from 'react-hot-toast';
import { useEffect } from 'react';
import { store } from './store';
import { apolloClient } from './services/apollo.client';
import { authService } from './services/auth.service';
import { LoginPage } from './features/auth/LoginPage';
import { RegisterPage } from './features/auth/RegisterPage';
import { ProtectedRoute } from './features/auth/ProtectedRoute';
import { MainLayout } from './components/layout/MainLayout';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { InventoryPage } from './features/inventory/InventoryPage';
import { UsersPage } from './features/users/UsersPage';
import { SalesPage } from './features/sales/SalesPage';
import { FinancialPage } from './features/financial/FinancialPage';
import { AnalyticsPage } from './features/analytics/AnalyticsPage';
import { DatabaseOverviewPage } from './features/database/components/DatabaseOverviewPage';
import { ShopPage } from './features/shop';

function App() {
  useEffect(() => {
    // Initialize automatic token refresh on app mount
    authService.initializeAutoRefresh();
  }, []);

  return (
    <Provider store={store}>
      <ApolloProvider client={apolloClient}>
        <BrowserRouter>
          <Toaster position="top-right" />
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route
              path="/"
              element={
                <ProtectedRoute>
                  <MainLayout />
                </ProtectedRoute>
              }
            >
              <Route index element={<Navigate to="/inventory" replace />} />
              <Route path="dashboard" element={<DashboardPage />} /> {/* Hidden from nav but route kept */}
              <Route path="users" element={<UsersPage />} />
              <Route path="inventory" element={<InventoryPage />} />
              <Route path="shop" element={<ShopPage />} />
              <Route path="sales" element={<SalesPage />} />
              <Route path="financial" element={<FinancialPage />} />
              <Route path="analytics" element={<AnalyticsPage />} />
              <Route path="database" element={<DatabaseOverviewPage />} />
            </Route>
            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </ApolloProvider>
    </Provider>
  );
}

export default App;
