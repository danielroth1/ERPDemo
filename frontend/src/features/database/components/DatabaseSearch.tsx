import React, { useState } from 'react';
import { databaseService } from '../../../services/database.service';
import type { DatabaseOverview, DatabaseSearchResult, SearchDatabaseRequest } from '../../../types/database.types';
import toast from 'react-hot-toast';

interface DatabaseSearchProps {
  overview: DatabaseOverview;
}

export const DatabaseSearch: React.FC<DatabaseSearchProps> = ({ overview }) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [filters, setFilters] = useState<Partial<SearchDatabaseRequest>>({});
  const [results, setResults] = useState<DatabaseSearchResult[]>([]);
  const [searching, setSearching] = useState(false);

  const handleSearch = async () => {
    setSearching(true);
    try {
      const request: SearchDatabaseRequest = {
        searchTerm: searchTerm || undefined,
        ...filters,
      };
      const data = await databaseService.searchDatabases(request);
      setResults(data);
      toast.success(`Found ${data.length} results`);
    } catch (error: any) {
      toast.error('Search failed');
    } finally {
      setSearching(false);
    }
  };

  const handleFilterChange = (field: keyof SearchDatabaseRequest, value: any) => {
    setFilters({ ...filters, [field]: value || undefined });
  };

  return (
    <div className="space-y-6">
      {/* Search Form */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-xl font-semibold mb-4">Search Databases</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {/* Search Term */}
          <div className="col-span-full">
            <label className="label">Search Term</label>
            <input
              type="text"
              className="input"
              placeholder="Search in collection names, fields..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            />
          </div>

          {/* Service Name Filter */}
          <div>
            <label className="label">Service</label>
            <select
              className="input"
              value={filters.serviceName || ''}
              onChange={(e) => handleFilterChange('serviceName', e.target.value)}
            >
              <option value="">All Services</option>
              {overview.services.map((service) => (
                <option key={service.serviceName} value={service.serviceName}>
                  {service.serviceName}
                </option>
              ))}
            </select>
          </div>

          {/* Collection Name Filter */}
          <div>
            <label className="label">Collection</label>
            <input
              type="text"
              className="input"
              placeholder="Collection name..."
              value={filters.collectionName || ''}
              onChange={(e) => handleFilterChange('collectionName', e.target.value)}
            />
          </div>

          {/* Document Count Range */}
          <div>
            <label className="label">Min Documents</label>
            <input
              type="number"
              className="input"
              placeholder="0"
              value={filters.minDocumentCount || ''}
              onChange={(e) => handleFilterChange('minDocumentCount', parseInt(e.target.value))}
            />
          </div>

          <div>
            <label className="label">Max Documents</label>
            <input
              type="number"
              className="input"
              placeholder="Unlimited"
              value={filters.maxDocumentCount || ''}
              onChange={(e) => handleFilterChange('maxDocumentCount', parseInt(e.target.value))}
            />
          </div>

          {/* Size Range */}
          <div>
            <label className="label">Min Size (bytes)</label>
            <input
              type="number"
              className="input"
              placeholder="0"
              value={filters.minSizeInBytes || ''}
              onChange={(e) => handleFilterChange('minSizeInBytes', parseInt(e.target.value))}
            />
          </div>

          <div>
            <label className="label">Max Size (bytes)</label>
            <input
              type="number"
              className="input"
              placeholder="Unlimited"
              value={filters.maxSizeInBytes || ''}
              onChange={(e) => handleFilterChange('maxSizeInBytes', parseInt(e.target.value))}
            />
          </div>
        </div>

        <div className="flex gap-3 mt-4">
          <button
            onClick={handleSearch}
            disabled={searching}
            className="btn btn-primary"
          >
            {searching ? 'Searching...' : 'Search'}
          </button>
          <button
            onClick={() => {
              setSearchTerm('');
              setFilters({});
              setResults([]);
            }}
            className="btn btn-secondary"
          >
            Clear
          </button>
        </div>
      </div>

      {/* Results */}
      {results.length > 0 && (
        <div className="bg-white rounded-lg shadow p-6">
          <h3 className="text-lg font-semibold mb-4">
            Search Results ({results.length})
          </h3>
          <div className="space-y-3">
            {results.map((result, index) => (
              <div
                key={index}
                className="border border-gray-200 rounded-lg p-4 hover:border-primary-300 transition-colors"
              >
                <div className="flex justify-between items-start">
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-semibold text-primary-600">
                        {result.serviceName}
                      </span>
                      <span className="text-gray-400">›</span>
                      <span className="text-sm text-gray-600">{result.databaseName}</span>
                      <span className="text-gray-400">›</span>
                      <span className="text-sm font-mono font-semibold text-gray-900">
                        {result.collectionName}
                      </span>
                    </div>
                    
                    <div className="flex gap-4 mt-2 text-xs text-gray-600">
                      <span>📄 {databaseService.formatNumber(result.documentCount)} documents</span>
                      <span>💾 {databaseService.formatBytes(result.sizeInBytes)}</span>
                    </div>

                    {result.matchedFields.length > 0 && (
                      <div className="mt-2">
                        <span className="text-xs text-gray-500">Matched fields: </span>
                        {result.matchedFields.map((field, idx) => (
                          <span
                            key={idx}
                            className="inline-block text-xs bg-yellow-100 text-yellow-800 px-2 py-0.5 rounded mr-1"
                          >
                            {field}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {results.length === 0 && searchTerm && !searching && (
        <div className="bg-white rounded-lg shadow p-6 text-center text-gray-500">
          No results found
        </div>
      )}
    </div>
  );
};
