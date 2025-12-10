import { useEffect, useState, useCallback } from 'react';
import { createClient } from 'graphql-ws';
import type { Client, SubscribePayload } from 'graphql-ws';
import type { DatabaseUpdateEvent } from '../types/database.types';

interface UseGraphQLSubscriptionOptions {
  url?: string;
  onData?: (data: DatabaseUpdateEvent) => void;
  onError?: (error: Error) => void;
}

export const useDatabaseSubscription = (options: UseGraphQLSubscriptionOptions = {}) => {
  const [client, setClient] = useState<Client | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [lastEvent, setLastEvent] = useState<DatabaseUpdateEvent | null>(null);

  const url = options.url || 'ws://localhost:5005/graphql';

  useEffect(() => {
    const wsClient = createClient({
      url,
      connectionParams: () => {
        const token = localStorage.getItem('token');
        return {
          authorization: token ? `Bearer ${token}` : '',
        };
      },
      on: {
        connected: () => setIsConnected(true),
        closed: () => setIsConnected(false),
      },
    });

    setClient(wsClient);

    return () => {
      wsClient.dispose();
    };
  }, [url]);

  const subscribe = useCallback(
    (subscriptionType: 'DatabaseUpdates' | 'DatabaseRefreshed' | 'QueryExecuted' = 'DatabaseUpdates') => {
      if (!client) return;

      const query = `
        subscription On${subscriptionType} {
          on${subscriptionType} {
            serviceName
            databaseName
            eventType
            collectionName
            timestamp
            metadata
          }
        }
      `;

      const unsubscribe = client.subscribe(
        { query } as SubscribePayload,
        {
          next: (data: any) => {
            const event = data.data?.[`on${subscriptionType}`];
            if (event) {
              setLastEvent(event);
              options.onData?.(event);
            }
          },
          error: (error: any) => {
            console.error('Subscription error:', error);
            options.onError?.(error);
          },
          complete: () => {
            console.log('Subscription complete');
          },
        }
      );

      return unsubscribe;
    },
    [client, options]
  );

  return {
    subscribe,
    isConnected,
    lastEvent,
    client,
  };
};

export default useDatabaseSubscription;
