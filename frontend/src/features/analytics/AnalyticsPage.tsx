import React, { useEffect } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/hooks';
import {
  fetchKPIs,
  fetchAlerts,
  fetchDashboardSummary,
  fetchTopProducts,
} from './analyticsSlice';
import { LoadingSpinner } from '../../components/common/LoadingSpinner';
import {
  ChartBarIcon,
  ArrowTrendingUpIcon,
  BellAlertIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
} from '@heroicons/react/24/outline';
import { AlertSeverity } from '../../types';

export const AnalyticsPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { kpis, alerts, dashboardSummary, topProducts, isLoading } = useAppSelector(
    (state) => state.analytics
  );

  useEffect(() => {
    dispatch(fetchKPIs());
    dispatch(fetchAlerts());
    dispatch(fetchDashboardSummary());
    dispatch(fetchTopProducts(5));

    // Refresh every 30 seconds for real-time updates
    const interval = setInterval(() => {
      dispatch(fetchKPIs());
      dispatch(fetchAlerts());
    }, 30000);

    return () => clearInterval(interval);
  }, [dispatch]);

  const getAlertIcon = (severity: string) => {
    switch (severity) {
      case AlertSeverity.Critical:
        return <ExclamationTriangleIcon className="h-5 w-5 text-red-600" />;
      case AlertSeverity.Warning:
        return <BellAlertIcon className="h-5 w-5 text-yellow-600" />;
      case AlertSeverity.Info:
        return <CheckCircleIcon className="h-5 w-5 text-blue-600" />;
      default:
        return <BellAlertIcon className="h-5 w-5 text-gray-600" />;
    }
  };

  const getAlertBgColor = (severity: string) => {
    switch (severity) {
      case AlertSeverity.Critical:
        return 'bg-red-50 border-red-200';
      case AlertSeverity.Warning:
        return 'bg-yellow-50 border-yellow-200';
      case AlertSeverity.Info:
        return 'bg-blue-50 border-blue-200';
      default:
        return 'bg-gray-50 border-gray-200';
    }
  };

  if (isLoading && kpis.length === 0) {
    return (
      <div className="flex justify-center items-center h-96">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Analytics Dashboard</h1>
        <div className="text-sm text-gray-500">
          Real-time updates • Last refresh: {new Date().toLocaleTimeString()}
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
        {kpis.slice(0, 4).map((kpi) => (
          <div key={kpi.id} className="card">
            <div className="flex items-center justify-between">
              <div className="flex-1">
                <p className="text-sm text-gray-600">{kpi.name}</p>
                <p className="text-2xl font-bold text-gray-900 mt-1">
                  {kpi.currentValue} {kpi.unit}
                </p>
                <div className="flex items-center mt-2">
                  {kpi.percentageChange >= 0 ? (
                    <ArrowTrendingUpIcon className="h-4 w-4 text-green-500 mr-1" />
                  ) : (
                    <ArrowTrendingUpIcon className="h-4 w-4 text-red-500 mr-1 transform rotate-180" />
                  )}
                  <span
                    className={`text-xs font-semibold ${
                      kpi.percentageChange >= 0 ? 'text-green-600' : 'text-red-600'
                    }`}
                  >
                    {Math.abs(kpi.percentageChange).toFixed(1)}%
                  </span>
                </div>
              </div>
              <div className="flex-shrink-0">
                <ChartBarIcon className="h-10 w-10 text-blue-500" />
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        {/* Recent Alerts */}
        <div className="card">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Recent Alerts</h2>
          <div className="space-y-3">
            {alerts.length === 0 ? (
              <p className="text-gray-500 text-sm text-center py-8">No active alerts</p>
            ) : (
              alerts.slice(0, 5).map((alert) => (
                <div
                  key={alert.id}
                  className={`p-3 rounded-lg border ${getAlertBgColor(alert.severity)}`}
                >
                  <div className="flex items-start">
                    <div className="flex-shrink-0">{getAlertIcon(alert.severity)}</div>
                    <div className="ml-3 flex-1">
                      <p className="text-sm font-medium text-gray-900">{alert.title}</p>
                      <p className="text-xs text-gray-600 mt-1">{alert.message}</p>
                      <p className="text-xs text-gray-400 mt-1">
                        {new Date(alert.createdAt).toLocaleString()}
                      </p>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Top Products */}
        <div className="card">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Top Selling Products</h2>
          <div className="space-y-3">
            {topProducts.length === 0 ? (
              <p className="text-gray-500 text-sm text-center py-8">No product data available</p>
            ) : (
              topProducts.map((product, index) => (
                <div
                  key={product.productId || index}
                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                >
                  <div className="flex items-center">
                    <div className="flex-shrink-0 w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                      <span className="text-sm font-bold text-blue-600">#{index + 1}</span>
                    </div>
                    <div className="ml-3">
                      <p className="text-sm font-medium text-gray-900">{product.productName}</p>
                      <p className="text-xs text-gray-500">{product.quantitySold || 0} units sold</p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-semibold text-gray-900">
                      ${(product.revenue || 0).toFixed(2)}
                    </p>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      {/* Summary Stats */}
      {dashboardSummary && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="card">
            <h3 className="text-sm font-medium text-gray-600 mb-2">Total Revenue</h3>
            <p className="text-3xl font-bold text-green-600">
              ${(dashboardSummary.totalRevenue || 0).toFixed(2)}
            </p>
            <p className="text-xs text-gray-500 mt-2">This month</p>
          </div>
          <div className="card">
            <h3 className="text-sm font-medium text-gray-600 mb-2">Total Orders</h3>
            <p className="text-3xl font-bold text-blue-600">{dashboardSummary.totalOrders || 0}</p>
            <p className="text-xs text-gray-500 mt-2">This month</p>
          </div>
          <div className="card">
            <h3 className="text-sm font-medium text-gray-600 mb-2">Total Customers</h3>
            <p className="text-3xl font-bold text-purple-600">
              {dashboardSummary.totalCustomers || 0}
            </p>
            <p className="text-xs text-gray-500 mt-2">Total active</p>
          </div>
        </div>
      )}
    </div>
  );
};
