import React, { useState } from 'react';
import { databaseService } from '../../../services/database.service';
import type { 
  ServiceDatabase, 
  QueryExecutionRequest, 
  QueryExecutionResponse 
} from '../../../types/database.types';
import toast from 'react-hot-toast';

interface QueryExecutorProps {
  services: ServiceDatabase[];
}

export const QueryExecutor: React.FC<QueryExecutorProps> = ({ services }) => {
  const [selectedService, setSelectedService] = useState('');
  const [selectedCollection, setSelectedCollection] = useState('');
  const [queryType, setQueryType] = useState<'Find' | 'Aggregate' | 'Count'>('Find');
  const [query, setQuery] = useState('{}');
  const [limit, setLimit] = useState(100);
  const [executing, setExecuting] = useState(false);
  const [result, setResult] = useState<QueryExecutionResponse | null>(null);

  const selectedServiceData = services.find(s => s.serviceName === selectedService);
  const collections = selectedServiceData?.collections || [];

  const queryTemplates = {
    Find: '{\n  "field": "value"\n}',
    Count: '{\n  "field": "value"\n}',
    Aggregate: '[\n  { "$match": { "field": "value" } },\n  { "$group": { "_id": "$field", "count": { "$sum": 1 } } }\n]',
  };

  const handleExecute = async () => {
    if (!selectedService || !selectedCollection) {
      toast.error('Please select a service and collection');
      return;
    }

    setExecuting(true);
    try {
      const request: QueryExecutionRequest = {
        databaseName: selectedServiceData!.databaseName,
        collectionName: selectedCollection,
        query,
        queryType,
        limit,
      };

      const data = await databaseService.executeQuery(request);
      setResult(data);

      if (data.isSuccessful) {
        toast.success(`Query executed in ${data.executionTimeMs}ms`);
      } else {
        toast.error(data.errorMessage || 'Query failed');
      }
    } catch (error: any) {
      toast.error(error?.message || 'Query execution failed');
      setResult(null);
    } finally {
      setExecuting(false);
    }
  };

  const handleTemplateChange = (type: 'Find' | 'Aggregate' | 'Count') => {
    setQueryType(type);
    setQuery(queryTemplates[type]);
  };

  return (
    <div className="space-y-6">
      {/* Query Builder */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold">Query Executor</h2>
          <span className="text-xs text-red-600 bg-red-50 px-2 py-1 rounded">
            Admin Only
          </span>
        </div>

        <div className="space-y-4">
          {/* Service and Collection Selection */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="label">Service</label>
              <select
                className="input"
                value={selectedService}
                onChange={(e) => {
                  setSelectedService(e.target.value);
                  setSelectedCollection('');
                }}
              >
                <option value="">Select a service</option>
                {services.filter(s => s.isConnected).map((service) => (
                  <option key={service.serviceName} value={service.serviceName}>
                    {service.serviceName}
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="label">Collection</label>
              <select
                className="input"
                value={selectedCollection}
                onChange={(e) => setSelectedCollection(e.target.value)}
                disabled={!selectedService}
              >
                <option value="">Select a collection</option>
                {collections.map((collection) => (
                  <option key={collection.name} value={collection.name}>
                    {collection.name}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Query Type and Limit */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="label">Query Type</label>
              <div className="flex gap-2">
                {(['Find', 'Count', 'Aggregate'] as const).map((type) => (
                  <button
                    key={type}
                    onClick={() => handleTemplateChange(type)}
                    className={`px-4 py-2 rounded text-sm font-medium ${
                      queryType === type
                        ? 'bg-primary-600 text-white'
                        : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
                    }`}
                  >
                    {type}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="label">Result Limit</label>
              <input
                type="number"
                className="input"
                value={limit}
                onChange={(e) => setLimit(parseInt(e.target.value) || 100)}
                min="1"
                max="1000"
              />
            </div>
          </div>

          {/* Query Editor */}
          <div>
            <label className="label">Query (JSON)</label>
            <textarea
              className="input font-mono text-sm"
              rows={10}
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Enter your MongoDB query..."
            />
          </div>

          {/* Actions */}
          <div className="flex gap-3">
            <button
              onClick={handleExecute}
              disabled={executing || !selectedService || !selectedCollection}
              className="btn btn-primary"
            >
              {executing ? 'Executing...' : 'Execute Query'}
            </button>
            <button
              onClick={() => setQuery(queryTemplates[queryType])}
              className="btn btn-secondary"
            >
              Reset to Template
            </button>
          </div>
        </div>
      </div>

      {/* Results */}
      {result && (
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold">
              {result.isSuccessful ? 'Query Results' : 'Query Error'}
            </h3>
            <div className="flex items-center gap-4 text-sm text-gray-600">
              <span>Results: {result.resultCount}</span>
              <span>Time: {result.executionTimeMs}ms</span>
              <span className={`px-2 py-1 rounded ${
                result.isSuccessful
                  ? 'bg-green-100 text-green-800'
                  : 'bg-red-100 text-red-800'
              }`}>
                {result.isSuccessful ? 'Success' : 'Failed'}
              </span>
            </div>
          </div>

          {result.isSuccessful ? (
            <div className="space-y-2">
              {result.results.length > 0 ? (
                result.results.map((doc, index) => (
                  <details key={index} className="border border-gray-200 rounded">
                    <summary className="cursor-pointer p-3 hover:bg-gray-50 font-mono text-sm">
                      Document {index + 1}
                    </summary>
                    <pre className="p-3 bg-gray-900 text-gray-100 text-xs overflow-x-auto">
                      {doc}
                    </pre>
                  </details>
                ))
              ) : (
                <div className="text-center text-gray-500 py-8">
                  No documents found
                </div>
              )}
            </div>
          ) : (
            <div className="bg-red-50 border border-red-200 rounded p-4">
              <p className="text-red-800 font-mono text-sm">{result.errorMessage}</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
