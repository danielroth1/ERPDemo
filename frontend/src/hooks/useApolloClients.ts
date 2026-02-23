import { useMemo } from 'react';
import { createApolloClient } from '../services/apollo.client';

export interface ApolloClients {
  dashboard: ReturnType<typeof createApolloClient>;
  sales: ReturnType<typeof createApolloClient>;
}

/**
 * Hook to access Apollo clients for different GraphQL endpoints
 * 
 * @example
 * const clients = useApolloClients();
 * const { data } = useQuery(GET_KPIS, { client: clients.dashboard });
 */
export function useApolloClients(): ApolloClients {
  return useMemo(() => ({
    dashboard: createApolloClient('dashboard'),
    sales: createApolloClient('sales'),
  }), []);
}
