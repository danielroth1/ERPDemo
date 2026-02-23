import React, { useEffect, useState } from "react";
import { gql } from "@apollo/client";
import { useQuery } from "@apollo/client/react";
import { signalRService } from "../../services/signalr.service";
import apiService from "../../services/api.service";
import { LoadingSpinner } from "../../components/common/LoadingSpinner";
import { useApolloClients } from "../../hooks/useApolloClients";
import type { DashboardMetrics, Alert as AlertType } from "../../types";
import toast from "react-hot-toast";

// GraphQL Response Types
interface KPI {
  id: string;
  name: string;
  description: string;
  currentValue: number;
  targetValue: number;
  previousValue: number;
  percentageChange: number;
  status: "OnTrack" | "AtRisk" | "Critical";
  lastUpdated: string;
}

interface Alert {
  id: string;
  title: string;
  message: string;
  severity: "Critical" | "Warning" | "Info";
  source: string;
  isRead: boolean;
  createdAt: string;
}

interface GetAllKPIsData {
  allKPIs: KPI[];
}

interface GetUnreadAlertsData {
  unreadAlerts: Alert[];
}

interface GetReadAlertsData {
  readAlerts: Alert[];
}

// GraphQL Query Example - fetches KPIs from Dashboard service
const GET_KPIS = gql`
  query GetAllKPIs {
    allKPIs {
      id
      name
      description
      currentValue
      targetValue
      previousValue
      percentageChange
      status
      lastUpdated
    }
  }
`;

// GraphQL Query Example - fetches unread alerts
const GET_UNREAD_ALERTS = gql`
  query GetUnreadAlerts {
    unreadAlerts {
      id
      title
      message
      severity
      source
      isRead
      createdAt
    }
  }
`;

const GET_READ_ALERTS = gql`
  query GetReadAlerts {
    unreadAlerts {
      id
      title
      message
      severity
      source
      data
      createdAt
    }
  }
`;

export const DashboardPage: React.FC = () => {
  const [metrics, setMetrics] = useState<DashboardMetrics | null>(null);
  const [alerts, setAlerts] = useState<AlertType[]>([]);
  const [loading, setLoading] = useState(true);

  // Get Apollo clients for different GraphQL endpoints
  const clients = useApolloClients();

  // GraphQL Example: Fetch KPIs using Apollo Client with Dashboard endpoint
  const {
    data: kpiData,
    loading: kpiLoading,
    error: kpiError,
  } = useQuery<GetAllKPIsData>(GET_KPIS, {
    client: clients.dashboard, // Use dashboard client
    pollInterval: 30000, // Poll every 30 seconds for updates
  });

  // GraphQL Example: Fetch unread alerts using Apollo Client with Dashboard endpoint
  const { data: alertData, loading: alertLoading } = useQuery<GetUnreadAlertsData>(
    GET_UNREAD_ALERTS,
    {
      client: clients.dashboard, // Use dashboard client
    }
  );

  const readAlertsResponse = useQuery<GetReadAlertsData>(
    GET_READ_ALERTS,
    {
      client: clients.dashboard
    }
  )

  useEffect(() => {
    loadDashboardData();
    connectToSignalR();

    return () => {
      signalRService.disconnect();
    };
  }, []);

  const loadDashboardData = async () => {
    try {
      const [metricsResponse, alertsResponse] = await Promise.all([
        apiService.get<{ data: DashboardMetrics }>("/api/v1/dashboard/metrics"),
        apiService.get<{ data: AlertType[] }>("/api/v1/alerts/unread"),
      ]);

      setMetrics(metricsResponse.data);
      setAlerts(alertsResponse.data);
    } catch (error: any) {
      toast.error("Failed to load dashboard data");
    } finally {
      setLoading(false);
    }
  };

  const connectToSignalR = async () => {
    try {
      await signalRService.connect();

      signalRService.on("dashboardUpdate", (data: DashboardMetrics) => {
        setMetrics(data);
      });

      signalRService.on("alert", (data: AlertType) => {
        setAlerts((prev) => [data, ...prev]);
        toast.error(data.message);
      });
    } catch (error) {
      console.error("SignalR connection failed:", error);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-96">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Dashboard</h1>

      {/* GraphQL Example Section */}
      <div className="card mb-8 bg-gradient-to-r from-purple-50 to-pink-50 border-2 border-purple-200">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-gray-900">
            🚀 GraphQL Apollo Client Example
          </h2>
          <span className="px-3 py-1 bg-purple-600 text-white text-xs font-medium rounded-full">
            Demo Only
          </span>
        </div>

        <div className="space-y-4">
          {/* KPIs from GraphQL */}
          <div className="bg-white p-4 rounded-lg shadow-sm">
            <h3 className="font-medium text-gray-700 mb-2">
              KPIs (via GraphQL)
            </h3>
            {kpiLoading ? (
              <p className="text-sm text-gray-500">Loading KPIs...</p>
            ) : kpiError ? (
              <div className="text-sm text-red-600">
                <p className="font-medium">GraphQL Error:</p>
                <p className="text-xs mt-1">
                  Failed to fetch KPIs from /dashboard/graphql endpoint.
                </p>
                <p className="text-xs mt-1 font-mono bg-red-50 p-2 rounded">
                  {kpiError.message}
                </p>
              </div>
            ) : kpiData?.allKPIs ? (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {kpiData.allKPIs.slice(0, 4).map((kpi: any) => (
                  <div
                    key={kpi.id}
                    className="border border-gray-200 p-3 rounded"
                  >
                    <p className="text-sm font-medium text-gray-900">
                      {kpi.name}
                    </p>
                    <p className="text-xs text-gray-500 mb-2">
                      {kpi.description}
                    </p>
                    <div className="flex items-baseline space-x-2">
                      <span className="text-lg font-bold text-gray-900">
                        {kpi.currentValue}
                      </span>
                      <span className="text-xs text-gray-500">
                        / {kpi.targetValue} target
                      </span>
                      <span
                        className={`text-xs font-medium ${
                          kpi.percentageChange >= 0
                            ? "text-green-600"
                            : "text-red-600"
                        }`}
                      >
                        {kpi.percentageChange > 0 ? "+" : ""}
                        {kpi.percentageChange}%
                      </span>
                    </div>
                    <p
                      className={`text-xs mt-1 font-medium ${
                        kpi.status === "OnTrack"
                          ? "text-green-600"
                          : kpi.status === "AtRisk"
                            ? "text-yellow-600"
                            : "text-red-600"
                      }`}
                    >
                      {kpi.status}
                    </p>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-gray-500">No KPIs available</p>
            )}
          </div>

          {/* Alerts from GraphQL */}
          <div className="bg-white p-4 rounded-lg shadow-sm">
            <h3 className="font-medium text-gray-700 mb-2">
              Unread Alerts (via GraphQL)
            </h3>
            {readAlertsResponse.loading ? (
              <p className="text-sm text-gray-500">Loading read alerts</p>
            ) : readAlertsResponse.data?.readAlerts ?
            (
              <div>
                {readAlertsResponse.data.readAlerts.map((alert: GetReadAlertsData) => (
                  <div></div>
                ))}
              </div>
            ) :
            (
              <div></div>
            )
            }
            {alertLoading ? (
              <p className="text-sm text-gray-500">Loading alerts...</p>
            ) : alertData?.unreadAlerts ? (
              <div className="space-y-2">
                {alertData.unreadAlerts.slice(0, 3).map((alert: any) => (
                  <div
                    key={alert.id}
                    className={`p-2 rounded text-xs ${
                      alert.severity === "Critical"
                        ? "bg-red-100 text-red-800"
                        : alert.severity === "Warning"
                          ? "bg-yellow-100 text-yellow-800"
                          : "bg-blue-100 text-blue-800"
                    }`}
                  >
                    <p className="font-medium">{alert.title}</p>
                    <p className="text-xs opacity-75">{alert.message}</p>
                  </div>
                ))}
                {alertData.unreadAlerts.length === 0 && (
                  <p className="text-sm text-gray-500">No unread alerts</p>
                )}
              </div>
            ) : (
              <p className="text-sm text-gray-500">No alerts available</p>
            )}
          </div>

          <div className="bg-purple-100 p-3 rounded text-xs text-purple-800">
            <p className="font-medium mb-1">💡 How this works:</p>
            <ul className="list-disc list-inside space-y-1 ml-2">
              <li>
                Uses <code className="bg-white px-1 rounded">useQuery</code>{" "}
                from @apollo/client with the{" "}
                <code className="bg-white px-1 rounded">client</code> option
              </li>
              <li>
                Each query specifies which client to use:{" "}
                <code className="bg-white px-1 rounded">
                  client: clients.dashboard
                </code>
              </li>
              <li>
                Queries Dashboard GraphQL endpoint at{" "}
                <code className="bg-white px-1 rounded">
                  /dashboard/graphql
                </code>
              </li>
              <li>Auto-updates every 30 seconds (pollInterval)</li>
              <li>
                Multiple Apollo clients supported without ApolloProvider nesting
              </li>
              <li>
                Other pages can use{" "}
                <code className="bg-white px-1 rounded">clients.sales</code> for
                /sales/graphql
              </li>
            </ul>
          </div>
        </div>
      </div>

      {/* Metrics Grid */}
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4 mb-8">
        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0 bg-blue-500 rounded-md p-3">
              <svg
                className="h-6 w-6 text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"
                />
              </svg>
            </div>
            <div className="ml-5 w-0 flex-1">
              <dl>
                <dt className="text-sm font-medium text-gray-500 truncate">
                  Total Users
                </dt>
                <dd className="text-2xl font-semibold text-gray-900">
                  {metrics?.totalUsers || 0}
                </dd>
              </dl>
            </div>
          </div>
        </div>

        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0 bg-green-500 rounded-md p-3">
              <svg
                className="h-6 w-6 text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
                />
              </svg>
            </div>
            <div className="ml-5 w-0 flex-1">
              <dl>
                <dt className="text-sm font-medium text-gray-500 truncate">
                  Total Products
                </dt>
                <dd className="text-2xl font-semibold text-gray-900">
                  {metrics?.totalProducts || 0}
                </dd>
              </dl>
            </div>
          </div>
        </div>

        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0 bg-yellow-500 rounded-md p-3">
              <svg
                className="h-6 w-6 text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z"
                />
              </svg>
            </div>
            <div className="ml-5 w-0 flex-1">
              <dl>
                <dt className="text-sm font-medium text-gray-500 truncate">
                  Total Orders
                </dt>
                <dd className="text-2xl font-semibold text-gray-900">
                  {metrics?.totalOrders || 0}
                </dd>
              </dl>
            </div>
          </div>
        </div>

        <div className="card">
          <div className="flex items-center">
            <div className="flex-shrink-0 bg-purple-500 rounded-md p-3">
              <svg
                className="h-6 w-6 text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <div className="ml-5 w-0 flex-1">
              <dl>
                <dt className="text-sm font-medium text-gray-500 truncate">
                  Total Revenue
                </dt>
                <dd className="text-2xl font-semibold text-gray-900">
                  ${metrics?.totalRevenue.toLocaleString() || 0}
                </dd>
              </dl>
            </div>
          </div>
        </div>
      </div>

      {/* Alerts */}
      {alerts.length > 0 && (
        <div className="card">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">
            Recent Alerts
          </h2>
          <div className="space-y-3">
            {alerts.slice(0, 5).map((alert) => (
              <div
                key={alert.id}
                className={`p-3 rounded-md ${
                  alert.severity === "Critical"
                    ? "bg-red-50 text-red-800"
                    : alert.severity === "Warning"
                      ? "bg-yellow-50 text-yellow-800"
                      : "bg-blue-50 text-blue-800"
                }`}
              >
                <p className="font-medium">{alert.title}</p>
                <p className="text-sm">{alert.message}</p>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
