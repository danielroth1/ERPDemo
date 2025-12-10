import React, { useEffect, useState } from 'react';
import { databaseService } from '../../../services/database.service';
import { useDatabaseSubscription } from '../../../hooks/useDatabaseSubscription';
import type { 
  DatabaseOverview, 
  DatabaseAlert
} from '../../../types/database.types';
import { DatabaseStats } from './DatabaseStats';
import { ServiceDatabaseCard } from './ServiceDatabaseCard';
import { DatabaseSearch } from './DatabaseSearch';
import { DatabaseAlerts } from './DatabaseAlerts';
import { QueryExecutor } from './QueryExecutor';
import toast from 'react-hot-toast';

export const DatabaseOverviewPage: React.FC = () => {
  const [overview, setOverview] = useState<DatabaseOverview | null>(null);
  const [alerts, setAlerts] = useState<DatabaseAlert[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedService, setSelectedService] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'overview' | 'search' | 'query' | 'alerts'>('overview');
  const [autoRefresh, setAutoRefresh] = useState(false);

  const { subscribe, isConnected } = useDatabaseSubscription({
    onData: (event) => {
      toast.success(`Database Update: ${event.eventType} on ${event.collectionName}`);
      if (autoRefresh) {
        loadOverview(false);
      }
    },
    onError: (error) => {
      console.error('Subscription error:', error);
    },
  });

  useEffect(() => {
    loadOverview();
    loadAlerts();
  }, []);

  useEffect(() => {
    if (isConnected) {
      const unsubscribe = subscribe('DatabaseUpdates');
      return () => unsubscribe?.();
    }
  }, [isConnected, subscribe]);

  const loadOverview = async (forceRefresh = false) => {
    try {
      setLoading(true);
      const data = await databaseService.getDatabaseOverview(forceRefresh, true);
      setOverview(data);
    } catch (error: any) {
      toast.error(error?.message || 'Failed to load database overview');
    } finally {
      setLoading(false);
    }
  };

  const loadAlerts = async () => {
    try {
      const data = await databaseService.getAlerts(false);
      setAlerts(data);
    } catch (error: any) {
      console.error('Failed to load alerts:', error);
    }
  };

  const handleRefresh = async () => {
    toast.promise(
      loadOverview(true),
      {
        loading: 'Refreshing database overview...',
        success: 'Database overview refreshed!',
        error: 'Failed to refresh',
      }
    );
  };

  const handleClearCache = async () => {
    try {
      await databaseService.clearCache();
      toast.success('Cache cleared successfully');
      await loadOverview(true);
    } catch (error: any) {
      toast.error('Failed to clear cache');
    }
  };

  if (loading && !overview) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (!overview) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-800">Failed to load database overview</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Database Management</h1>
          <p className="text-gray-600 mt-1">
            MongoDB Database Overview & Management
            {isConnected && (
              <span className="ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">
                <span className="w-2 h-2 bg-green-400 rounded-full mr-1 animate-pulse"></span>
                Live
              </span>
            )}
          </p>
        </div>
        <div className="flex gap-2">
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={autoRefresh}
              onChange={(e) => setAutoRefresh(e.target.checked)}
              className="rounded border-gray-300"
            />
            Auto-refresh
          </label>
          <button
            onClick={handleClearCache}
            className="btn btn-secondary"
          >
            Clear Cache
          </button>
          <button
            onClick={handleRefresh}
            disabled={loading}
            className="btn btn-primary"
          >
            {loading ? 'Refreshing...' : 'Refresh'}
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          {(['overview', 'search', 'query', 'alerts'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`
                py-2 px-1 border-b-2 font-medium text-sm capitalize
                ${activeTab === tab
                  ? 'border-primary-500 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }
              `}
            >
              {tab}
              {tab === 'alerts' && alerts.length > 0 && (
                <span className="ml-2 bg-red-100 text-red-600 py-0.5 px-2 rounded-full text-xs">
                  {alerts.length}
                </span>
              )}
            </button>
          ))}
        </nav>
      </div>

      {/* Content */}
      {activeTab === 'overview' && (
        <div className="space-y-6">
          {/* Stats Cards */}
          <DatabaseStats stats={overview.totalStats} />

          {/* Service Databases */}
          <div className="space-y-4">
            <h2 className="text-xl font-semibold">Service Databases</h2>
            {overview.services.map((service) => (
              <ServiceDatabaseCard
                key={service.serviceName}
                service={service}
                isExpanded={selectedService === service.serviceName}
                onToggle={() => setSelectedService(
                  selectedService === service.serviceName ? null : service.serviceName
                )}
              />
            ))}
          </div>
        </div>
      )}

      {activeTab === 'search' && (
        <DatabaseSearch overview={overview} />
      )}

      {activeTab === 'query' && (
        <QueryExecutor services={overview.services} />
      )}

      {activeTab === 'alerts' && (
        <DatabaseAlerts alerts={alerts} onRefresh={loadAlerts} />
      )}
    </div>
  );
};
