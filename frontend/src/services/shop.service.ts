// Shop Service for customer-facing product operations
import { createInventoryClient } from '../generated/clients/inventory/inventoryClient';
import { createOrchestrationClient } from '../generated/clients/orchestration/orchestrationClient';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { BearerTokenAuthenticationProvider } from './auth/bearer-token-provider';
import { extractErrorMessage } from '../utils/error-handler';
import { getApiBaseUrl } from './api-base-url';
import type {
  ProductResponse,
  CategoryResponse,
} from '../generated/clients/inventory/models';

const API_BASE_URL = getApiBaseUrl();

// Re-export types from generated models for convenience
export type ShopProduct = ProductResponse;
export type ShopCategory = CategoryResponse;

class ShopService {
  private inventoryClient;
  private orchestrationClient;

  constructor() {
    const authProvider = new BearerTokenAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = API_BASE_URL;
    this.inventoryClient = createInventoryClient(adapter);

    const orchestrationAdapter = new FetchRequestAdapter(authProvider);
    orchestrationAdapter.baseUrl = API_BASE_URL;
    this.orchestrationClient = createOrchestrationClient(orchestrationAdapter);
  }

  /**
   * Get all available products for shopping
   */
  async getAvailableProducts(categoryId?: string): Promise<ShopProduct[]> {
    try {
      const response = await this.inventoryClient.api.v1.shop.products.get({
        queryParameters: categoryId ? { categoryId } : undefined
      });
      return response?.data || [];
    } catch (error) {
      throw new Error(extractErrorMessage(error, 'Failed to load products'));
    }
  }

  /**
   * Get all categories for filtering
   */
  async getCategories(): Promise<ShopCategory[]> {
    try {
      const response = await this.inventoryClient.api.v1.shop.categories.get();
      return response?.data || [];
    } catch (error) {
      throw new Error(extractErrorMessage(error, 'Failed to load categories'));
    }
  }

  /**
   * Purchase a product (reduces stock)
   */
  async purchaseProduct(productId: string, quantity: number = 1): Promise<unknown> {
    try {
      const response = await this.orchestrationClient.api.v1.shop.purchase.byProductId(productId).post({
        queryParameters: { quantity }
      });
      return response?.data || response;
    } catch (error) {
      throw new Error(extractErrorMessage(error, 'Failed to purchase product'));
    }
  }

  /**
   * Return a product (increases stock)
   */
  async returnProduct(productId: string, quantity: number = 1): Promise<unknown> {
    try {
      const response = await this.orchestrationClient.api.v1.shop.returnEscaped.byProductId(productId).post({
        queryParameters: { quantity }
      });
      return response?.data || response;
    } catch (error) {
      throw new Error(extractErrorMessage(error, 'Failed to return product'));
    }
  }
}

export const shopService = new ShopService();

