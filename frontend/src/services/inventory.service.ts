import { inventoryApiClient } from './inventory-api.client';
import type { Product, Category, StockMovement, PaginatedResponse } from '../types';
import type { ProductResponse, CategoryResponse, StockMovementResponse } from '../generated/clients/inventory/models';

// Map Kiota types to legacy types
function mapProductResponse(product: ProductResponse): Product {
  return {
    id: product.id || '',
    name: product.name || '',
    description: product.description || '',
    sku: product.sku || '',
    categoryId: product.categoryId || '',
    unitPrice: product.price || 0,
    stockQuantity: product.stockQuantity || 0,
    reorderLevel: product.minStockLevel || 0,
    isActive: product.isActive ?? true,
    createdAt: product.createdAt?.toISOString() || new Date().toISOString(),
    updatedAt: product.updatedAt?.toISOString() || new Date().toISOString(),
  };
}

function mapCategoryResponse(category: CategoryResponse): Category {
  return {
    id: category.id || '',
    name: category.name || '',
    description: category.description || '',
    createdAt: category.createdAt?.toISOString() || new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
}

function mapStockMovementResponse(movement: StockMovementResponse): StockMovement {
  return {
    id: movement.id || '',
    productId: movement.productId || '',
    movementType: movement.movementType as any,
    quantity: movement.quantity || 0,
    reference: movement.reference || '',
    notes: movement.notes || '',
    createdAt: movement.createdAt?.toISOString() || new Date().toISOString(),
  };
}

class InventoryService {
  // Products
  async getProducts(page: number = 1, pageSize: number = 10): Promise<PaginatedResponse<Product>> {
    const response = await inventoryApiClient.getProducts(page, pageSize);
    
    return {
      items: response.items?.map(mapProductResponse) || [],
      page: response.page || page,
      pageSize: response.pageSize || pageSize,
      totalCount: response.totalCount || 0,
      totalPages: response.totalPages || 1,
    };
  }

  async getProduct(id: string): Promise<Product> {
    const product = await inventoryApiClient.getProduct(id);
    return mapProductResponse(product);
  }

  async createProduct(product: Omit<Product, 'id' | 'createdAt' | 'updatedAt'>): Promise<Product> {
    const response = await inventoryApiClient.createProduct({
      name: product.name,
      description: product.description,
      sku: product.sku,
      categoryId: product.categoryId,
      price: product.unitPrice,
      stockQuantity: product.stockQuantity,
      minStockLevel: product.reorderLevel,
    });
    return mapProductResponse(response);
  }

  async updateProduct(id: string, product: Partial<Product>): Promise<Product> {
    const response = await inventoryApiClient.updateProduct(id, {
      name: product.name,
      description: product.description,
      sku: product.sku,
      categoryId: product.categoryId,
      price: product.unitPrice,
      stockQuantity: product.stockQuantity,
      minStockLevel: product.reorderLevel,
    });
    return mapProductResponse(response);
  }

  async deleteProduct(id: string): Promise<void> {
    await inventoryApiClient.deleteProduct(id);
  }

  async searchProducts(query: string): Promise<Product[]> {
    const products = await inventoryApiClient.searchProducts(query);
    return products.map(mapProductResponse);
  }

  async getLowStockProducts(): Promise<Product[]> {
    await inventoryApiClient.getLowStockAlerts();
    // Convert alerts to products (would need product details from alerts)
    return [];
  }

  // Categories
  async getCategories(): Promise<Category[]> {
    const categories = await inventoryApiClient.getCategories();
    return categories.map(mapCategoryResponse);
  }

  async createCategory(category: Omit<Category, 'id' | 'createdAt' | 'updatedAt'>): Promise<Category> {
    const response = await inventoryApiClient.createCategory({
      name: category.name,
      description: category.description,
    });
    return mapCategoryResponse(response);
  }

  async updateCategory(id: string, category: Partial<Category>): Promise<Category> {
    const response = await inventoryApiClient.updateCategory(id, {
      name: category.name,
      description: category.description,
    });
    return mapCategoryResponse(response);
  }

  async deleteCategory(id: string): Promise<void> {
    await inventoryApiClient.deleteCategory(id);
  }

  async getCategoryProductCount(categoryId: string): Promise<number> {
    return await inventoryApiClient.getCategoryProductCount(categoryId);
  }

  // Stock Movements
  async getStockMovements(productId?: string): Promise<StockMovement[]> {
    const movements = await inventoryApiClient.getStockMovements(productId);
    return movements.map(mapStockMovementResponse);
  }

  async recordStockMovement(movement: Omit<StockMovement, 'id' | 'createdAt'>): Promise<StockMovement> {
    const response = await inventoryApiClient.createStockMovement({
      productId: movement.productId,
      movementType: movement.movementType as any,
      quantity: movement.quantity,
      reference: movement.reference,
      notes: movement.notes,
    });
    return mapStockMovementResponse(response);
  }

  // Seed products
  async seedProducts(): Promise<{ productsCreated: number; productsDeleted: number }> {
    return await inventoryApiClient.seedProducts();
  }
}

export const inventoryService = new InventoryService();
export default inventoryService;
