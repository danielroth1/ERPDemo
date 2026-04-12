import { ApolloClient, InMemoryCache, HttpLink, ApolloLink } from '@apollo/client';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { getMainDefinition } from '@apollo/client/utilities';
import { createClient } from 'graphql-ws';
import { SetContextLink } from '@apollo/client/link/context';
import { getApiBaseUrl } from './api-base-url';

/**
 * Create an HTTP link for a specific GraphQL service
 * @param service - The service name (e.g., 'dashboard', 'sales')
 */
function createHttpLink(service: string): HttpLink {
  return new HttpLink({
    uri: `${getApiBaseUrl()}/${service}/graphql`,
  });
}

/**
 * Create a WebSocket link for GraphQL subscriptions
 * @param service - The service name (e.g., 'dashboard', 'sales')
 */
function createWsLink(service: string): GraphQLWsLink {
  const wsBase = (() => {
    if (import.meta.env.VITE_WS_URL) return import.meta.env.VITE_WS_URL as string;
    if (import.meta.env.PROD) {
      const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
      return `${proto}//${window.location.host}`;
    }
    return 'ws://localhost:5001';
  })();
  return new GraphQLWsLink(
    createClient({
      url: `${wsBase}/${service}/graphql`,
      connectionParams: () => {
        const token = localStorage.getItem('accessToken');
        return {
          authorization: token ? `Bearer ${token}` : '',
        };
      },
    })
  );
}

/**
 * Authentication link to add JWT token to requests
 */
const authLink = new SetContextLink(({ headers }, _) => {
  const token = localStorage.getItem('accessToken');
  return {
    headers: {
      ...headers,
      authorization: token ? `Bearer ${token}` : '',
    },
  };
});

/**
 * Create a split link that routes subscriptions to WebSocket and queries/mutations to HTTP
 * @param service - The service name (e.g., 'dashboard', 'sales')
 */
function createSplitLink(service: string): ApolloLink {
  const wsLink = createWsLink(service);
  const httpLink = createHttpLink(service);

  return ApolloLink.split(
    ({ query }) => {
      const definition = getMainDefinition(query);
      return (
        definition.kind === 'OperationDefinition' &&
        definition.operation === 'subscription'
      );
    },
    wsLink,
    authLink.concat(httpLink)
  );
}

/**
 * Create an Apollo Client for a specific GraphQL service
 * @param service - The service name (e.g., 'dashboard', 'sales')
 * @returns Configured Apollo Client instance
 * 
 * @example
 * const dashboardClient = createApolloClient('dashboard');
 * const { data } = useQuery(MY_QUERY, { client: dashboardClient });
 */
export function createApolloClient(service: string) {
  return new ApolloClient({
    link: createSplitLink(service),
    cache: new InMemoryCache(),
    defaultOptions: {
      watchQuery: {
        fetchPolicy: 'cache-and-network',
      },
    },
  });
}
