import React from 'react';
import { databaseService } from '../../../services/database.service';
import type { DatabaseStats as DatabaseStatsType } from '../../../types/database.types';

interface DatabaseStatsProps {
  stats: DatabaseStatsType;
}

export const DatabaseStats: React.FC<DatabaseStatsProps> = ({ stats }) => {
  const statCards = [
    {
      label: 'Total Collections',
      value: databaseService.formatNumber(stats.totalCollections),
      icon: '📁',
      color: 'bg-blue-500',
    },
    {
      label: 'Total Documents',
      value: databaseService.formatNumber(stats.totalDocuments),
      icon: '📄',
      color: 'bg-green-500',
    },
    {
      label: 'Total Size',
      value: databaseService.formatBytes(stats.totalSizeInBytes),
      icon: '💾',
      color: 'bg-purple-500',
    },
    {
      label: 'Total Indexes',
      value: databaseService.formatNumber(stats.totalIndexes),
      icon: '🔍',
      color: 'bg-orange-500',
    },
    {
      label: 'Avg Document Size',
      value: databaseService.formatBytes(stats.averageDocumentSize),
      icon: '📊',
      color: 'bg-pink-500',
    },
  ];

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
      {statCards.map((stat, index) => (
        <div key={index} className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-600 mb-1">{stat.label}</p>
              <p className="text-2xl font-bold text-gray-900">{stat.value}</p>
            </div>
            <div className={`${stat.color} w-12 h-12 rounded-lg flex items-center justify-center text-2xl`}>
              {stat.icon}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};
