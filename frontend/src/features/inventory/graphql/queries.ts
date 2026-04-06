import { gql } from '@apollo/client';
import type { TypedDocumentNode } from '@apollo/client';

// ---------- shared shapes ----------

export interface GqlProductItem {
  id: string;
  sku: string;
  name: string;
  description: string;
  categoryId: string;
  price: number;
  cost?: number;
  stockQuantity: number;
  reservedQuantity: number;
  minStockLevel: number;
  maxStockLevel: number;
  unit: string;
  isActive: boolean;
  imageUrl: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface GqlProductDocument {
  id: string;
  productId: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedBy: string;
  uploadedAt: string;
}

// ---------- GetProducts ----------

export interface GetProductsData {
  products: {
    items: GqlProductItem[];
    totalCount: number;
  };
}

export interface GetProductsVariables {
  skip?: number;
  take?: number;
}

// ---------- GetProductWithDocuments ----------

export interface GetProductWithDocumentsData {
  product: (GqlProductItem & { documents: GqlProductDocument[] }) | null;
}

export interface GetProductWithDocumentsVariables {
  id: string;
}

/**
 * Fetches a paginated list of products.
 * Uses HotChocolate's UseOffsetPaging — pass skip=(page-1)*take and take=pageSize.
 * Field-level projections are applied server-side; only the requested fields are fetched from the DB.
 */
export const GET_PRODUCTS: TypedDocumentNode<GetProductsData, GetProductsVariables> = gql`
  query GetProducts($skip: Int, $take: Int) {
    products(skip: $skip, take: $take) {
      items {
        id
        sku
        name
        description
        categoryId
        price
        stockQuantity
        reservedQuantity
        minStockLevel
        maxStockLevel
        unit
        isActive
        imageUrl
        createdAt
        updatedAt
      }
      totalCount
    }
  }
`;

/**
 * Fetches a single product with its documents.
 * Documents are resolved via a batch DataLoader — all sibling products' documents
 * are loaded in one DB round-trip even when this query runs inside a list context.
 */
export const GET_PRODUCT_WITH_DOCUMENTS: TypedDocumentNode<GetProductWithDocumentsData, GetProductWithDocumentsVariables> = gql`
  query GetProductWithDocuments($id: String!) {
    product(id: $id) {
      id
      sku
      name
      description
      categoryId
      price
      cost
      stockQuantity
      reservedQuantity
      minStockLevel
      maxStockLevel
      unit
      isActive
      imageUrl
      createdAt
      updatedAt
      documents {
        id
        productId
        originalFileName
        contentType
        sizeBytes
        uploadedBy
        uploadedAt
      }
    }
  }
`;
