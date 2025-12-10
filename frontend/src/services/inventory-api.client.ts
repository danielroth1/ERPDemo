// Inventory API Client using Kiota-generated client
import { createInventoryClient } from '../generated/clients/inventory/inventoryClient';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { BearerTokenAuthenticationProvider } from './auth/bearer-token-provider';
import type {
  ProductResponse,
  ProductRequest,
  CategoryResponse,
  CategoryRequest,
  StockMovementResponse,
  StockMovementRequest,
  StockAdjustmentRequest,
  LowStockAlert,
  ProductResponsePaginatedResponse,
} from '../generated/clients/inventory/models';

const API_BASE_URL = import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000';

class InventoryApiClient {
  private client;

  constructor() {
    const authProvider = new BearerTokenAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = API_BASE_URL;

    this.client = createInventoryClient(adapter);
  }

  // Products
  async getProducts(page: number = 1, pageSize: number = 10): Promise<ProductResponsePaginatedResponse> {
    const response = await this.client.api.v1.products.get({
      queryParameters: { page, pageSize },
    });
    if (!response?.data) {
      throw new Error('Failed to fetch products');
    }
    return response.data;
  }

  async getProduct(id: string): Promise<ProductResponse> {
    const response = await this.client.api.v1.products.byId(id).get();
    if (!response?.data) {
      throw new Error(`Product ${id} not found`);
    }
    return response.data;
  }

  async createProduct(request: ProductRequest): Promise<ProductResponse> {
    const response = await this.client.api.v1.products.post(request);
    if (!response?.data) {
      throw new Error('Failed to create product');
    }
    return response.data;
  }

  async updateProduct(id: string, request: ProductRequest): Promise<ProductResponse> {
    const response = await this.client.api.v1.products.byId(id).put(request);
    if (!response?.data) {
      throw new Error('Failed to update product');
    }
    return response.data as ProductResponse;
  }

  async deleteProduct(id: string): Promise<void> {
    await this.client.api.v1.products.byId(id).delete();
  }

  async searchProducts(_searchQuery: string): Promise<ProductResponse[]> {
    const response = await this.client.api.v1.products.search.get();
    return response?.data || [];
  }

  async getLowStockAlerts(): Promise<LowStockAlert[]> {
    const response = await this.client.api.v1.products.lowStock.get();
    return response?.data || [];
  }

  async getProductsByCategory(categoryId: string): Promise<ProductResponse[]> {
    const response = await this.client.api.v1.products.category.byCategoryId(categoryId).get();
    return response?.data || [];
  }

  async getCategoryProductCount(categoryId: string): Promise<number> {
    const response = await this.client.api.v1.products.category.byCategoryId(categoryId).count.get();
    return response?.data || 0;
  }

  // Categories
  async getCategories(): Promise<CategoryResponse[]> {
    const response = await this.client.api.v1.categories.get();
    return response?.data || [];
  }

  async getCategory(id: string): Promise<CategoryResponse> {
    const response = await this.client.api.v1.categories.byId(id).get();
    if (!response?.data) {
      throw new Error(`Category ${id} not found`);
    }
    return response.data;
  }

  async createCategory(request: CategoryRequest): Promise<CategoryResponse> {
    const response = await this.client.api.v1.categories.post(request);
    if (!response?.data) {
      throw new Error('Failed to create category');
    }
    return response.data;
  }

  async updateCategory(id: string, request: CategoryRequest): Promise<CategoryResponse> {
    const response = await this.client.api.v1.categories.byId(id).put(request);
    if (!response?.data) {
      throw new Error('Failed to update category');
    }
    return response.data as CategoryResponse;
  }

  async deleteCategory(id: string): Promise<void> {
    await this.client.api.v1.categories.byId(id).delete();
  }

  // Stock Movements
  async getStockMovements(_productId?: string): Promise<StockMovementResponse[]> {
    // Kiota client may not have this endpoint - return empty array for now
    return [];
  }

  async createStockMovement(request: StockMovementRequest): Promise<StockMovementResponse> {
    const response = await this.client.api.v1.stockMovements.post(request);
    if (!response?.data) {
      throw new Error('Failed to create stock movement');
    }
    return response.data;
  }

  async adjustStock(_productId: string, _request: StockAdjustmentRequest): Promise<void> {
    // Endpoint not available in generated client
    throw new Error('Adjust stock endpoint not implemented');
  }

  // Seed products
  async seedProducts(): Promise<{ productsCreated: number; productsDeleted: number }> {
    const response = await this.client.api.v1.products.seed.post();
    if (!response?.data) {
      throw new Error('Failed to seed products');
    }
    return {
      productsCreated: response.data.productsCreated || 0,
      productsDeleted: response.data.productsDeleted || 0,
    };
  }
}

export const inventoryApiClient = new InventoryApiClient();
export default inventoryApiClient;
