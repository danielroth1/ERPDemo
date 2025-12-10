import { dashboardApiClient } from './dashboard-api.client';
import type { 
  KPIResponse,
  AlertResponse,
  TopProductResponse 
} from '../generated/clients/dashboard/models';

// Map Kiota types to legacy types for backward compatibility
interface KPI {
  id: string;
  name: string;
  description: string;
  targetValue: number;
  currentValue: number;
  previousValue: number;
  percentageChange: number;
  unit: string;
  status: string;
  startDate: string;
  endDate: string;
  createdAt: string;
  updatedAt: string;
}

interface Alert {
  id: string;
  title: string;
  message: string;
  severity: string;
  isRead: boolean;
  source: string;
  createdAt: string;
}

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
    status: kpi.status || '',
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
    severity: alert.severity || '',
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

  async getDashboardSummary(): Promise<any> {
    const metrics = await dashboardApiClient.getDashboardMetrics();
    // Convert Date objects to strings for Redux serialization
    return {
      ...metrics,
      lastUpdated: metrics.lastUpdated?.toISOString() || new Date().toISOString(),
    };
  }

  async getTopProducts(limit: number = 5): Promise<TopProductResponse[]> {
    return await dashboardApiClient.getTopProducts(limit);
  }

  async getRevenueChart(_period: string = 'month'): Promise<any> {
    // TODO: Implement with Kiota client when endpoint is available
    throw new Error('Revenue chart endpoint not yet implemented');
  }
}

export const analyticsService = new AnalyticsService();
export default analyticsService;
