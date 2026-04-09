// Database Service using Kiota-generated Dashboard client
import { createDashboardClient } from '../generated/clients/dashboard/dashboardClient';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { BearerTokenAuthenticationProvider } from './auth/bearer-token-provider';
import { getApiBaseUrl } from './api-base-url';
import type {
  DatabaseOverview,
  ServiceDatabase,
  DatabaseSearchResult,
  SearchDatabaseRequest,
  QueryExecutionRequest,
  QueryExecutionResponse,
  QueryExecutionHistory,
  DatabaseAlert,
} from '../types/database.types';

const API_BASE_URL = getApiBaseUrl();

class DatabaseService {
  private client;

  constructor() {
    const authProvider = new BearerTokenAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = API_BASE_URL;

    this.client = createDashboardClient(adapter);
  }

  /**
   * Get complete database overview for all services
   */
  async getDatabaseOverview(forceRefresh = false, includeSampleDocuments = true): Promise<DatabaseOverview> {
    const response = await this.client.api.v1.database.overview.get({
      queryParameters: { forceRefresh, includeSampleDocuments }
    });
    if (!response) throw new Error('Failed to fetch database overview');
    return response as unknown as DatabaseOverview; // Type mapping between Kiota and local types
  }

  /**
   * Get database info for a specific service
   */
  async getServiceDatabase(serviceName: string, forceRefresh = false): Promise<ServiceDatabase> {
    const response = await this.client.api.v1.database.service.byServiceName(serviceName).get({
      queryParameters: { forceRefresh }
    });
    if (!response) throw new Error('Failed to fetch service database');
    return response as unknown as ServiceDatabase;
  }

  /**
   * Search across all databases and collections
   */
  async searchDatabases(request: SearchDatabaseRequest): Promise<DatabaseSearchResult[]> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const response = await this.client.api.v1.database.search.post(request as any);
    if (!response) throw new Error('Failed to search databases');
    return response as unknown as DatabaseSearchResult[];
  }

  /**
   * Execute a MongoDB query (Admin only)
   */
  async executeQuery(request: QueryExecutionRequest): Promise<QueryExecutionResponse> {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const response = await this.client.api.v1.database.query.post(request as any);
    if (!response) throw new Error('Failed to execute query');
    return response as unknown as QueryExecutionResponse;
  }

  /**
   * Get query execution history
   */
  async getQueryHistory(onlyMyQueries = false, limit = 50): Promise<QueryExecutionHistory[]> {
    const response = await this.client.api.v1.database.queryHistory.get({
      queryParameters: { onlyMyQueries, limit }
    });
    if (!response) throw new Error('Failed to fetch query history');
    return response as unknown as QueryExecutionHistory[];
  }

  /**
   * Get database alerts
   */
  async getAlerts(includeResolved = false): Promise<DatabaseAlert[]> {
    const response = await this.client.api.v1.database.alerts.get({
      queryParameters: { includeResolved }
    });
    if (!response) throw new Error('Failed to fetch alerts');
    return response as unknown as DatabaseAlert[];
  }

  /**
   * Clear cache and force refresh
   */
  async clearCache(): Promise<{ message: string }> {
    const response = await this.client.api.v1.database.cache.clear.post({});
    if (!response) throw new Error('Failed to clear cache');
    return response as unknown as { message: string };
  }

  /**
   * Format bytes to human-readable format
   */
  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }

  /**
   * Format number with commas
   */
  formatNumber(num: number): string {
    return num.toLocaleString();
  }

  /**
   * Get severity color class
   */
  getSeverityColor(severity: string): string {
    switch (severity.toLowerCase()) {
      case 'critical':
        return 'text-red-600 bg-red-100';
      case 'warning':
        return 'text-yellow-600 bg-yellow-100';
      case 'info':
        return 'text-blue-600 bg-blue-100';
      default:
        return 'text-gray-600 bg-gray-100';
    }
  }
}

export const databaseService = new DatabaseService();
export default databaseService;
