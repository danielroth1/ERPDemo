// Database Management Types
export interface DatabaseOverview {
  id: string;
  generatedAt: string;
  services: ServiceDatabase[];
  totalStats: DatabaseStats;
  cacheTimeSeconds: number;
}

export interface ServiceDatabase {
  serviceName: string;
  databaseName: string;
  connectionString: string;
  port: number;
  collections: CollectionInfo[];
  stats: DatabaseStats;
  isConnected: boolean;
  errorMessage?: string;
}

export interface CollectionInfo {
  name: string;
  documentCount: number;
  sizeInBytes: number;
  averageSizeInBytes: number;
  indexes: IndexInfo[];
  sampleDocument?: string;
  schema?: Record<string, string>;
}

export interface IndexInfo {
  name: string;
  keys: Record<string, number>;
  isUnique: boolean;
  isSparse: boolean;
  sizeInBytes: number;
}

export interface DatabaseStats {
  totalCollections: number;
  totalDocuments: number;
  totalSizeInBytes: number;
  totalIndexes: number;
  averageDocumentSize: number;
}

export interface QueryExecutionRequest {
  databaseName: string;
  collectionName: string;
  query: string;
  queryType: 'Find' | 'Aggregate' | 'Count';
  limit?: number;
  skip?: number;
}

export interface QueryExecutionResponse {
  id: string;
  isSuccessful: boolean;
  errorMessage?: string;
  results: string[];
  resultCount: number;
  executionTimeMs: number;
  executedAt: string;
}

export interface QueryExecutionHistory {
  id: string;
  userEmail: string;
  databaseName: string;
  collectionName: string;
  query: string;
  queryType: string;
  isSuccessful: boolean;
  resultCount: number;
  executionTimeMs: number;
  executedAt: string;
}

export interface DatabaseAlert {
  id: string;
  serviceName: string;
  databaseName: string;
  collectionName: string;
  alertType: string;
  message: string;
  severity: 'Info' | 'Warning' | 'Critical';
  metadata: Record<string, any>;
  isResolved: boolean;
  createdAt: string;
  resolvedAt?: string;
}

export interface SearchDatabaseRequest {
  searchTerm?: string;
  serviceName?: string;
  collectionName?: string;
  minDocumentCount?: number;
  maxDocumentCount?: number;
  minSizeInBytes?: number;
  maxSizeInBytes?: number;
}

export interface DatabaseSearchResult {
  serviceName: string;
  databaseName: string;
  collectionName: string;
  documentCount: number;
  sizeInBytes: number;
  matchedFields: string[];
}

export interface DatabaseUpdateEvent {
  serviceName: string;
  databaseName: string;
  eventType: string;
  collectionName: string;
  timestamp: string;
  metadata?: Record<string, any>;
}
