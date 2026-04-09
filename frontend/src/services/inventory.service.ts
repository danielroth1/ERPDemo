import { inventoryApiClient } from './inventory-api.client';
import { createApolloClient } from './apollo.client';
import { GET_PRODUCTS, GET_PRODUCT_WITH_DOCUMENTS } from '../features/inventory/graphql/queries';
import type { GqlProductItem, GqlProductDocument } from '../features/inventory/graphql/queries';
import { getApiBaseUrl } from './api-base-url';
import type { Product, Category, StockMovement, PaginatedResponse, ProductDocument } from '../types';
import type { ProductResponse, CategoryResponse, StockMovementResponse } from '../generated/clients/inventory/models';

// Singleton Apollo client for inventory GraphQL reads
const inventoryGqlClient = createApolloClient('inventory');

// Map GraphQL response (camelCase from HotChocolate) to frontend Product type
function mapGqlProduct(p: GqlProductItem & { documents?: GqlProductDocument[] }): Product {
  return {
    id: p.id,
    name: p.name,
    description: p.description,
    sku: p.sku,
    categoryId: p.categoryId,
    unitPrice: p.price,
    stockQuantity: p.stockQuantity,
    reorderLevel: p.minStockLevel,
    unit: p.unit,
    isActive: p.isActive,
    imageUrl: p.imageUrl,
    documents: (p.documents ?? []).map((d: GqlProductDocument): ProductDocument => ({
      id: d.id,
      productId: d.productId,
      originalFileName: d.originalFileName,
      contentType: d.contentType,
      sizeBytes: d.sizeBytes,
      uploadedBy: d.uploadedBy,
      uploadedAt: d.uploadedAt,
    })),
    createdAt: p.createdAt,
    updatedAt: p.updatedAt,
  };
}

// Map Kiota types to legacy types
type ExtendedProductResponse = ProductResponse & {
  unit?: string;
  imageUrl?: string | null;
  documents?: Array<{
    id?: string;
    productId?: string;
    originalFileName?: string;
    contentType?: string;
    sizeBytes?: number;
    uploadedBy?: string;
    uploadedAt?: string;
  }>;
};

function mapProductResponse(product: ProductResponse): Product {
  const p = product as ExtendedProductResponse;
  return {
    id: p.id || '',
    name: p.name || '',
    description: p.description || '',
    sku: p.sku || '',
    categoryId: p.categoryId || '',
    unitPrice: p.price || 0,
    stockQuantity: p.stockQuantity || 0,
    reorderLevel: p.minStockLevel || 0,
    unit: p.unit ?? 'pcs',
    isActive: p.isActive ?? true,
    imageUrl: p.imageUrl ?? null,
    documents: (p.documents ?? []).map((d): ProductDocument => ({
      id: d.id || '',
      productId: d.productId || '',
      originalFileName: d.originalFileName || '',
      contentType: d.contentType || '',
      sizeBytes: d.sizeBytes || 0,
      uploadedBy: d.uploadedBy || '',
      uploadedAt: d.uploadedAt?.toISOString() ?? new Date().toISOString(),
    })),
    createdAt: p.createdAt?.toISOString() || new Date().toISOString(),
    updatedAt: p.updatedAt?.toISOString() || new Date().toISOString(),
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
    movementType: movement.movementType as unknown as StockMovement['movementType'],
    quantity: movement.quantity || 0,
    reference: movement.reference || '',
    notes: movement.notes || '',
    createdAt: movement.createdAt?.toISOString() || new Date().toISOString(),
  };
}

class InventoryService {
  // Products — reads via GraphQL (projections + batch DataLoader)
  async getProducts(page: number = 1, pageSize: number = 10): Promise<PaginatedResponse<Product>> {
    const skip = (page - 1) * pageSize;
    const { data } = await inventoryGqlClient.query({
      query: GET_PRODUCTS,
      variables: { skip, take: pageSize },
      fetchPolicy: 'network-only',
    });
    const segment = data?.products;
    return {
      items: (segment?.items ?? []).map(mapGqlProduct),
      page,
      pageSize,
      totalCount: segment?.totalCount ?? 0,
      totalPages: Math.ceil((segment?.totalCount ?? 0) / pageSize),
    };
  }

  async getProduct(id: string): Promise<Product> {
    const { data } = await inventoryGqlClient.query({
      query: GET_PRODUCT_WITH_DOCUMENTS,
      variables: { id },
      fetchPolicy: 'network-only',
    });
    if (!data?.product) throw new Error(`Product ${id} not found`);
    return mapGqlProduct(data.product);
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
      movementType: movement.movementType as unknown as StockMovementResponse['movementType'],
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

  // --- File / Media methods ---

  private getAuthHeaders(): Record<string, string> {
    const token = localStorage.getItem('accessToken');
    return token ? { Authorization: `Bearer ${token}` } : {};
  }

  private fileUrl(productId: string, suffix: string): string {
    return `${getApiBaseUrl()}/api/v1/productfiles/${productId}${suffix}`;
  }

  uploadImage(
    productId: string,
    file: File,
    onProgress: (percent: number) => void,
    signal: AbortSignal,
  ): Promise<string> {
    return new Promise((resolve, reject) => {
      const form = new FormData();
      form.append('file', file);

      const xhr = new XMLHttpRequest();
      signal.addEventListener('abort', () => xhr.abort());

      xhr.upload.onprogress = (e) => {
        if (e.lengthComputable) onProgress(Math.round((e.loaded / e.total) * 100));
      };
      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          const body = JSON.parse(xhr.responseText);
          resolve(body.data as string);
        } else {
          const msg = (() => { try { return JSON.parse(xhr.responseText)?.message; } catch { return xhr.statusText; } })();
          reject(new Error(msg || 'Upload failed'));
        }
      };
      xhr.onerror = () => reject(new Error('Network error during upload'));
      xhr.onabort = () => reject(new DOMException('Upload cancelled', 'AbortError'));

      xhr.open('POST', this.fileUrl(productId, '/image'));
      const headers = this.getAuthHeaders();
      Object.entries(headers).forEach(([k, v]) => xhr.setRequestHeader(k, v));
      xhr.send(form);
    });
  }

  async deleteImage(productId: string): Promise<void> {
    const res = await fetch(this.fileUrl(productId, '/image'), {
      method: 'DELETE',
      headers: this.getAuthHeaders(),
    });
    if (!res.ok) throw new Error('Failed to delete image');
  }

  uploadDocument(
    productId: string,
    file: File,
    onProgress: (percent: number) => void,
    signal: AbortSignal,
  ): Promise<ProductDocument> {
    return new Promise((resolve, reject) => {
      const form = new FormData();
      form.append('file', file);

      const xhr = new XMLHttpRequest();
      signal.addEventListener('abort', () => xhr.abort());

      xhr.upload.onprogress = (e) => {
        if (e.lengthComputable) onProgress(Math.round((e.loaded / e.total) * 100));
      };
      xhr.onload = () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          const body = JSON.parse(xhr.responseText);
          const d = body.data;
          resolve({
            id: d.id,
            productId: d.productId,
            originalFileName: d.originalFileName,
            contentType: d.contentType,
            sizeBytes: d.sizeBytes,
            uploadedBy: d.uploadedBy,
            uploadedAt: d.uploadedAt,
          });
        } else {
          const msg = (() => { try { return JSON.parse(xhr.responseText)?.message; } catch { return xhr.statusText; } })();
          reject(new Error(msg || 'Upload failed'));
        }
      };
      xhr.onerror = () => reject(new Error('Network error during upload'));
      xhr.onabort = () => reject(new DOMException('Upload cancelled', 'AbortError'));

      xhr.open('POST', this.fileUrl(productId, '/documents'));
      const headers = this.getAuthHeaders();
      Object.entries(headers).forEach(([k, v]) => xhr.setRequestHeader(k, v));
      xhr.send(form);
    });
  }

  async getDocuments(productId: string): Promise<ProductDocument[]> {
    const res = await fetch(this.fileUrl(productId, '/documents'), {
      headers: this.getAuthHeaders(),
    });
    if (!res.ok) throw new Error('Failed to fetch documents');
    const body = await res.json();
    return (body.data ?? []).map((d: Record<string, unknown>): ProductDocument => ({
      id: String(d.id),
      productId: String(d.productId),
      originalFileName: String(d.originalFileName),
      contentType: String(d.contentType),
      sizeBytes: Number(d.sizeBytes),
      uploadedBy: String(d.uploadedBy),
      uploadedAt: String(d.uploadedAt),
    }));
  }

  getDocumentDownloadUrl(productId: string, docId: string): string {
    return this.fileUrl(productId, `/documents/${docId}/download`);
  }

  async deleteDocument(productId: string, docId: string): Promise<void> {
    const res = await fetch(this.fileUrl(productId, `/documents/${docId}`), {
      method: 'DELETE',
      headers: this.getAuthHeaders(),
    });
    if (!res.ok) throw new Error('Failed to delete document');
  }
}

export const inventoryService = new InventoryService();
export default inventoryService;
