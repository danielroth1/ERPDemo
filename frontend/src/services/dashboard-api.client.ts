// Dashboard API Client using Kiota-generated client
import { createDashboardClient } from '../generated/clients/dashboard/dashboardClient';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { BearerTokenAuthenticationProvider } from './auth/bearer-token-provider';
import type {
  DashboardMetricsResponse,
  KPIResponse,
  AlertResponse,
  TopProductResponse,
  SalesOverviewResponse,
} from '../generated/clients/dashboard/models';

const API_BASE_URL = import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000';

class DashboardApiClient {
  private client;

  constructor() {
    const authProvider = new BearerTokenAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = API_BASE_URL;

    this.client = createDashboardClient(adapter);
  }

  async getDashboardMetrics(): Promise<DashboardMetricsResponse> {
    const response = await this.client.api.v1.dashboard.metrics.get();
    if (!response?.data) {
      throw new Error('Failed to fetch dashboard metrics');
    }
    return response.data;
  }

  async getKPIs(): Promise<KPIResponse[]> {
    const response = await this.client.api.v1.kPIs.get();
    if (!response?.data) {
      throw new Error('Failed to fetch KPIs');
    }
    return response.data;
  }

  async getAlerts(): Promise<AlertResponse[]> {
    const response = await this.client.api.v1.alerts.get();
    if (!response?.data) {
      throw new Error('Failed to fetch alerts');
    }
    return response.data;
  }

  async getSalesOverview(): Promise<SalesOverviewResponse> {
    const response = await this.client.api.v1.dashboard.sales.get();
    if (!response?.data) {
      throw new Error('Failed to fetch sales overview');
    }
    return response.data;
  }

  async getTopProducts(limit: number = 5): Promise<TopProductResponse[]> {
    const salesOverview = await this.getSalesOverview();
    return salesOverview.topProducts?.slice(0, limit) || [];
  }
}

export const dashboardApiClient = new DashboardApiClient();
export default dashboardApiClient;
