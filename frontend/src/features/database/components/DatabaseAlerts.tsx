import React from 'react';
import { databaseService } from '../../../services/database.service';
import type { DatabaseAlert } from '../../../types/database.types';

interface DatabaseAlertsProps {
  alerts: DatabaseAlert[];
  onRefresh: () => void;
}

export const DatabaseAlerts: React.FC<DatabaseAlertsProps> = ({ alerts, onRefresh }) => {
  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold">Database Alerts</h2>
        <button onClick={onRefresh} className="btn btn-secondary">
          Refresh
        </button>
      </div>

      {/* Alerts List */}
      {alerts.length > 0 ? (
        <div className="space-y-3">
          {alerts.map((alert) => (
            <div
              key={alert.id}
              className="bg-white rounded-lg shadow border-l-4 p-4"
              style={{
                borderLeftColor:
                  alert.severity === 'Critical' ? '#EF4444' :
                  alert.severity === 'Warning' ? '#F59E0B' :
                  '#3B82F6'
              }}
            >
              <div className="flex items-start justify-between">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2">
                    <span className={`px-2 py-1 rounded text-xs font-medium ${
                      databaseService.getSeverityColor(alert.severity)
                    }`}>
                      {alert.severity}
                    </span>
                    <span className="text-sm font-semibold text-gray-900">
                      {alert.alertType}
                    </span>
                    {alert.isResolved && (
                      <span className="px-2 py-1 rounded text-xs font-medium bg-green-100 text-green-800">
                        Resolved
                      </span>
                    )}
                  </div>

                  <p className="text-sm text-gray-700 mb-2">{alert.message}</p>

                  <div className="flex gap-4 text-xs text-gray-600">
                    <span>📦 {alert.serviceName}</span>
                    <span>🗄️ {alert.databaseName}</span>
                    <span>📁 {alert.collectionName}</span>
                    <span>🕒 {new Date(alert.createdAt).toLocaleString()}</span>
                  </div>

                  {Object.keys(alert.metadata).length > 0 && (
                    <details className="mt-2">
                      <summary className="text-xs text-gray-600 cursor-pointer">
                        View Metadata
                      </summary>
                      <pre className="mt-1 text-xs bg-gray-100 p-2 rounded overflow-x-auto">
                        {JSON.stringify(alert.metadata, null, 2)}
                      </pre>
                    </details>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <div className="bg-white rounded-lg shadow p-12 text-center">
          <div className="text-6xl mb-4">✅</div>
          <p className="text-gray-600">No active alerts</p>
          <p className="text-sm text-gray-500 mt-1">All databases are operating normally</p>
        </div>
      )}
    </div>
  );
};
