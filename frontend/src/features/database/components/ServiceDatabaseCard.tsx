import React, { useState } from 'react';
import { databaseService } from '../../../services/database.service';
import type { ServiceDatabase } from '../../../types/database.types';

interface ServiceDatabaseCardProps {
  service: ServiceDatabase;
  isExpanded: boolean;
  onToggle: () => void;
}

export const ServiceDatabaseCard: React.FC<ServiceDatabaseCardProps> = ({
  service,
  isExpanded,
  onToggle,
}) => {
  const [selectedCollection, setSelectedCollection] = useState<string | null>(null);

  const toggleCollection = (collectionName: string) => {
    setSelectedCollection(selectedCollection === collectionName ? null : collectionName);
  };

  return (
    <div className="bg-white rounded-lg shadow overflow-hidden">
      {/* Service Header */}
      <div
        className="p-6 cursor-pointer hover:bg-gray-50 transition-colors"
        onClick={onToggle}
      >
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <div className="flex-shrink-0">
              <div className={`w-12 h-12 rounded-lg flex items-center justify-center ${
                service.isConnected ? 'bg-green-100' : 'bg-red-100'
              }`}>
                <span className="text-2xl">🗄️</span>
              </div>
            </div>
            <div>
              <h3 className="text-lg font-semibold text-gray-900">{service.serviceName}</h3>
              <p className="text-sm text-gray-600">{service.databaseName}</p>
              {service.errorMessage && (
                <p className="text-xs text-red-600 mt-1">{service.errorMessage}</p>
              )}
            </div>
          </div>
          <div className="flex items-center gap-6">
            <div className="text-right">
              <p className="text-sm text-gray-500">Collections</p>
              <p className="text-lg font-semibold">{service.stats.totalCollections}</p>
            </div>
            <div className="text-right">
              <p className="text-sm text-gray-500">Documents</p>
              <p className="text-lg font-semibold">
                {databaseService.formatNumber(service.stats.totalDocuments)}
              </p>
            </div>
            <div className="text-right">
              <p className="text-sm text-gray-500">Size</p>
              <p className="text-lg font-semibold">
                {databaseService.formatBytes(service.stats.totalSizeInBytes)}
              </p>
            </div>
            <span className={`px-3 py-1 rounded-full text-sm font-medium ${
              service.isConnected
                ? 'bg-green-100 text-green-800'
                : 'bg-red-100 text-red-800'
            }`}>
              {service.isConnected ? '● Connected' : '● Disconnected'}
            </span>
            <svg
              className={`w-5 h-5 text-gray-400 transition-transform ${
                isExpanded ? 'transform rotate-180' : ''
              }`}
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </div>
        </div>
      </div>

      {/* Expanded Collections */}
      {isExpanded && service.isConnected && (
        <div className="border-t border-gray-200 bg-gray-50 p-6">
          <h4 className="font-semibold mb-4 text-gray-700">Collections</h4>
          {service.collections.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <p>No collections found in this database</p>
            </div>
          ) : (
            <div className="space-y-3">
              {service.collections.map((collection) => (
                <div key={collection.name} className="bg-white rounded-lg border border-gray-200">
                  <div
                    className="p-4 cursor-pointer hover:bg-gray-50"
                    onClick={() => toggleCollection(collection.name)}
                  >
                    <div className="flex justify-between items-center">
                      <div>
                        <h5 className="font-mono text-sm font-semibold text-blue-600">
                          {collection.name}
                        </h5>
                        <div className="flex gap-4 mt-1 text-xs text-gray-600">
                          <span>📄 {databaseService.formatNumber(collection.documentCount)} docs</span>
                          <span>💾 {databaseService.formatBytes(collection.sizeInBytes)}</span>
                          <span>🔍 {collection.indexes.length} indexes</span>
                          <span>📊 Avg: {databaseService.formatBytes(collection.averageSizeInBytes)}</span>
                        </div>
                      </div>
                      <svg
                        className={`w-4 h-4 text-gray-400 transition-transform ${
                          selectedCollection === collection.name ? 'transform rotate-180' : ''
                        }`}
                        fill="none"
                        stroke="currentColor"
                        viewBox="0 0 24 24"
                      >
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                      </svg>
                    </div>
                  </div>

                  {/* Collection Details */}
                  {selectedCollection === collection.name && (
                    <div className="border-t border-gray-200 p-4 space-y-4">
                      {/* Indexes */}
                      <div>
                        <p className="text-sm font-semibold text-gray-700 mb-2">Indexes:</p>
                        {collection.indexes.length === 0 ? (
                          <p className="text-xs text-gray-500">No indexes defined</p>
                        ) : (
                          <div className="flex flex-wrap gap-2">
                            {collection.indexes.map((index) => {
                              // Convert keys object to readable string
                              const keysStr = Object.entries(index.keys)
                                .map(([field, direction]) => `${field}: ${direction === 1 ? 'asc' : 'desc'}`)
                                .join(', ');
                              
                              return (
                                <div
                                  key={index.name}
                                  className="inline-flex items-center gap-1 text-xs bg-blue-50 text-blue-700 px-3 py-1 rounded-full border border-blue-200"
                                  title={keysStr}
                                >
                                  <span className="font-mono">{index.name}</span>
                                  {index.isUnique && (
                                    <span className="text-xs bg-blue-200 px-1 rounded">unique</span>
                                  )}
                                  {index.isSparse && (
                                    <span className="text-xs bg-blue-200 px-1 rounded">sparse</span>
                                  )}
                                </div>
                              );
                            })}
                          </div>
                        )}
                      </div>

                      {/* Schema */}
                      <div>
                        <p className="text-sm font-semibold text-gray-700 mb-2">Schema:</p>
                        {!collection.schema || Object.keys(collection.schema).length === 0 ? (
                          <p className="text-xs text-gray-500">
                            {collection.documentCount === 0 
                              ? 'No documents in collection - schema cannot be inferred' 
                              : 'Schema not available'}
                          </p>
                        ) : (
                          <div className="bg-gray-800 text-gray-100 p-3 rounded text-xs font-mono overflow-x-auto">
                            {Object.entries(collection.schema).map(([field, type]) => (
                              <div key={field} className="flex gap-2">
                                <span className="text-blue-400">{field}:</span>
                                <span className="text-green-400">
                                  {typeof type === 'string' ? type : JSON.stringify(type)}
                                </span>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>

                      {/* Sample Documents */}
                      {collection.documentCount > 0 && collection.sampleDocument ? (
                        <details>
                          <summary className="text-sm font-semibold text-gray-700 cursor-pointer hover:text-gray-900">
                            Sample Documents (up to 20)
                          </summary>
                          <pre className="mt-2 text-xs bg-gray-900 text-gray-100 p-3 rounded overflow-x-auto max-h-96 overflow-y-auto">
                            {typeof collection.sampleDocument === 'string'
                              ? collection.sampleDocument
                              : JSON.stringify(collection.sampleDocument, null, 2)}
                          </pre>
                        </details>
                      ) : (
                        <div>
                          <p className="text-sm font-semibold text-gray-700 mb-2">Sample Documents:</p>
                          <p className="text-xs text-gray-500">No documents available</p>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
