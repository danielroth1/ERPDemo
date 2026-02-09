import { ApolloClient, InMemoryCache, HttpLink, split, ApolloLink } from '@apollo/client';
import { GraphQLWsLink } from '@apollo/client/link/subscriptions';
import { getMainDefinition } from '@apollo/client/utilities';
import { createClient } from 'graphql-ws';
import { setContext } from '@apollo/client/link/context';

// GraphQL endpoint via API Gateway for Dashboard service
const httpLink = new HttpLink({
  uri: `${import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5001'}/dashboard/graphql`,
});

function createHttpLink(target: string): HttpLink {
  return new HttpLink({
    uri: `${import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5001'}/dashboard/${target}`,
  });
}

// WebSocket for GraphQL subscriptions (direct to Dashboard service)
const wsLink = new GraphQLWsLink(
  createClient({
    url: `${import.meta.env.VITE_WS_URL || 'ws://localhost:5006'}/graphql`,
    connectionParams: () => {
      const token = localStorage.getItem('accessToken');
      return {
        authorization: token ? `Bearer ${token}` : '',
      };
    },
  })
);

const authLink = setContext((_, { headers }) => {
  const token = localStorage.getItem('accessToken');
  return {
    headers: {
      ...headers,
      authorization: token ? `Bearer ${token}` : '',
    },
  };
});

function createSplitLink(target: string) : ApolloLink {
  return split(
    ({ query }) => {
      const definition = getMainDefinition(query);
      return (
        definition.kind === 'OperationDefinition' &&
        definition.operation === 'subscription'
      );
    },
    wsLink,
    authLink.concat(createHttpLink(target)));
}

const splitLink = split(
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

export const apolloClient = new ApolloClient({
  link: splitLink,
  cache: new InMemoryCache(),
  defaultOptions: {
    watchQuery: {
      fetchPolicy: 'cache-and-network',
    },
  },
});

export function createApolloClient(target: string) : ApolloClient {
  return new ApolloClient({
    link: createSplitLink(target),
    cache: new InMemoryCache(),
    defaultOptions: {
      watchQuery: {
        fetchPolicy: 'cache-and-network',
      },
    },
  });
}