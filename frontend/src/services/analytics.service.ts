import { dashboardApiClient } from './dashboard-api.client';
import type { 
  KPIResponse,
  AlertResponse,
  TopProductResponse,
  DashboardMetricsResponse,
} from '../generated/clients/dashboard/models';
import type { KPI, Alert, KPIStatus, AlertSeverity } from '../types';

function mapKPIResponse(kpi: KPIResponse): KPI {
  return {
    id: kpi.id || '',
    name: kpi.name || '',
    description: kpi.description || '',
    targetValue: kpi.targetValue || 0,
    currentValue: kpi.currentValue || 0,
    previousValue: kpi.previousValue || 0,
    percentageChange: kpi.percentageChange || 0,
    unit: '',
    status: (kpi.status || 'OnTrack') as KPIStatus,
    startDate: '',
    endDate: '',
    createdAt: kpi.lastUpdated?.toISOString() || new Date().toISOString(),
    updatedAt: kpi.lastUpdated?.toISOString() || new Date().toISOString(),
  };
}

function mapAlertResponse(alert: AlertResponse): Alert {
  return {
    id: alert.id || '',
    title: alert.title || '',
    message: alert.message || '',
    severity: (alert.severity || 'Info') as AlertSeverity,
    isRead: alert.isRead || false,
    source: alert.source || '',
    createdAt: alert.createdAt?.toISOString() || new Date().toISOString(),
  };
}

class AnalyticsService {
  async getKPIs(): Promise<KPI[]> {
    const kpis = await dashboardApiClient.getKPIs();
    return kpis.map(mapKPIResponse);
  }

  async getAlerts(): Promise<Alert[]> {
    const alerts = await dashboardApiClient.getAlerts();
    return alerts.map(mapAlertResponse);
  }

  async getDashboardSummary(): Promise<DashboardMetricsResponse> {
    return await dashboardApiClient.getDashboardMetrics();
  }

  async getTopProducts(limit: number = 5): Promise<TopProductResponse[]> {
    return await dashboardApiClient.getTopProducts(limit);
  }

  async getRevenueChart(_period: string = 'month'): Promise<unknown> {
    // TODO: Implement with Kiota client when endpoint is available
    throw new Error('Revenue chart endpoint not yet implemented');
  }
}

export const analyticsService = new AnalyticsService();
export default analyticsService;
