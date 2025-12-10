# API Gateway Service

The API Gateway serves as the single entry point for all client applications to access the ERP system microservices. It provides centralized authentication, authorization, rate limiting, routing, load balancing, and monitoring capabilities using YARP (Yet Another Reverse Proxy).

## Features

### Reverse Proxy (YARP)
- **Unified Entry Point**: Single endpoint for all backend services
- **Dynamic Routing**: Route requests to appropriate microservices based on path
- **Path Transformation**: Rewrite paths when forwarding to backend services
- **Load Balancing**: Distribute traffic across multiple service instances
- **Health Checks**: Active health monitoring of backend services every 10 seconds

### Authentication & Authorization
- **JWT Bearer Authentication**: Validates tokens for all protected routes
- **Centralized Auth**: Single authentication middleware for all services
- **Role-Based Access**: Forwards user context to backend services
- **Public Routes**: Auth endpoints accessible without tokens
- **Token Forwarding**: Passes JWT tokens to downstream services

### Rate Limiting
- **IP-Based Throttling**: Limit requests per IP address
- **Configurable Limits**: 100 requests/minute, 1000 requests/hour per IP
- **Endpoint-Specific Rules**: Different limits for different routes
- **Localhost Exemption**: Higher limits for local development
- **429 Status Code**: Standard rate limit exceeded response

### Cross-Origin Resource Sharing (CORS)
- **Frontend Support**: Configured for React development servers
- **Flexible Origins**: Supports localhost:3000 and localhost:5173
- **Credentials Support**: Allows cookies and authentication headers
- **Any Method/Header**: Full flexibility for API calls

### Monitoring & Observability
- **Prometheus Metrics**: HTTP request metrics at `/metrics`
- **Structured Logging**: Serilog with compact JSON format
- **Request Logging**: Automatic logging of all proxied requests
- **Health Endpoints**: `/health/live` and `/health/ready`

### Resilience
- **Circuit Breaker**: Polly integration for fault tolerance (configured but not active in basic setup)
- **Automatic Retries**: Failed request retry logic
- **Timeout Management**: Request timeout configuration
- **Graceful Degradation**: Continue serving healthy services

## Architecture

### Technologies
- **ASP.NET Core 8**: Web framework
- **YARP 2.3**: Microsoft's reverse proxy library
- **AspNetCoreRateLimit 5.0**: Rate limiting middleware
- **JWT Bearer Authentication**: Token validation
- **Polly 8.6**: Resilience and transient-fault-handling
- **Prometheus**: Metrics collection
- **Serilog**: Structured logging

### Service Routing

```
┌─────────────┐
│   Client    │
│ (React App) │
└──────┬──────┘
       │ HTTP/HTTPS
       ▼
┌─────────────────┐
│  API Gateway    │
│  (Port 8080)    │
│                 │
│ - JWT Auth      │
│ - Rate Limit    │
│ - CORS          │
│ - Routing       │
└────────┬────────┘
         │
    ┌────┴────┬────────┬────────┬──────────┐
    │         │        │        │          │
    ▼         ▼        ▼        ▼          ▼
┌────────┐ ┌──────┐ ┌─────┐ ┌────────┐ ┌─────────┐
│  User  │ │Inven-│ │Sales│ │Financial│ │Dashboard│
│  Mgmt  │ │tory  │ │     │ │        │ │         │
└────────┘ └──────┘ └─────┘ └────────┘ └─────────┘
```

## Route Configuration

### User Management Routes
| Pattern | Backend | Auth Required | Description |
|---------|---------|---------------|-------------|
| `/api/v1/auth/**` | user-management:8080 | ❌ | Login, register, logout |
| `/api/v1/users/**` | user-management:8080 | ✅ | User CRUD operations |

### Inventory Routes
| Pattern | Backend | Auth Required | Description |
|---------|---------|---------------|-------------|
| `/api/v1/products/**` | inventory:8080 | ✅ | Product management |
| `/api/v1/categories/**` | inventory:8080 | ✅ | Category management |
| `/api/v1/stock-movements/**` | inventory:8080 | ✅ | Stock tracking |

### Sales & Orders Routes
| Pattern | Backend | Auth Required | Description |
|---------|---------|---------------|-------------|
| `/api/v1/orders/**` | sales:8080 | ✅ | Order management |
| `/api/v1/customers/**` | sales:8080 | ✅ | Customer management |
| `/api/v1/invoices/**` | sales:8080 | ✅ | Invoice management |
| `/sales/graphql/**` | sales:8080/graphql | ✅ | GraphQL API |

### Financial Management Routes
| Pattern | Backend | Auth Required | Description |
|---------|---------|---------------|-------------|
| `/api/v1/accounts/**` | financial:8080 | ✅ | Account management |
| `/api/v1/transactions/**` | financial:8080 | ✅ | Transaction management |
| `/api/v1/budgets/**` | financial:8080 | ✅ | Budget management |
| `/api/v1/reports/**` | financial:8080 | ✅ | Financial reports |

### Dashboard & Analytics Routes
| Pattern | Backend | Auth Required | Description |
|---------|---------|---------------|-------------|
| `/api/v1/dashboard/**` | dashboard:8080 | ✅ | Dashboard metrics |
| `/api/v1/kpis/**` | dashboard:8080 | ✅ | KPI management |
| `/api/v1/alerts/**` | dashboard:8080 | ✅ | Alert management |
| `/dashboard/graphql/**` | dashboard:8080/graphql | ✅ | GraphQL API |
| `/dashboardHub/**` | dashboard:8080/dashboardHub | ✅ | SignalR WebSocket |

## Configuration

### appsettings.json Structure

#### JWT Settings
```json
{
  "Jwt": {
    "Secret": "your-secret-key",
    "Issuer": "erp-user-service",
    "Audience": "erp-clients"
  }
}
```

#### Service Endpoints
```json
{
  "Services": {
    "UserManagement": "http://user-management:8080",
    "Inventory": "http://inventory:8080",
    "Sales": "http://sales:8080",
    "Financial": "http://financial:8080",
    "Dashboard": "http://dashboard:8080"
  }
}
```

#### YARP Route Example
```json
{
  "ReverseProxy": {
    "Routes": {
      "user-auth-route": {
        "ClusterId": "user-cluster",
        "Match": {
          "Path": "/api/v1/auth/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "user-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://user-management:8080"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:10",
            "Path": "/health/ready"
          }
        }
      }
    }
  }
}
```

#### Rate Limiting Configuration
```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 1000
      }
    ]
  }
}
```

## Health Checks

### Active Health Monitoring
- **Interval**: Every 10 seconds
- **Timeout**: 5 seconds
- **Path**: `/health/ready` on each backend service
- **Action**: Automatically removes unhealthy destinations from load balancing

### Gateway Health Endpoints
- **Liveness**: `GET /health/live` - Gateway itself is running
- **Readiness**: `GET /health/ready` - Gateway is ready to serve traffic

## Rate Limiting Rules

### Default Rules
- **100 requests per minute** per IP address
- **1000 requests per hour** per IP address
- **Status Code**: 429 (Too Many Requests)

### Localhost Exception
- **1000 requests per minute** for 127.0.0.1 (development)

### Custom Rules
Can be configured per endpoint in `IpRateLimitPolicies` section.

## Authentication Flow

1. **Client** sends request with `Authorization: Bearer {token}` header
2. **API Gateway** validates JWT token against configured secret
3. **If valid**: Request proxied to backend service with token forwarded
4. **If invalid**: Returns 401 Unauthorized
5. **If missing** (protected route): Returns 401 Unauthorized
6. **If missing** (public route): Request proxied without authentication

## CORS Policy

### Allowed Origins
- `http://localhost:3000` (Create React App default)
- `http://localhost:5173` (Vite default)

### Allowed Methods
- All HTTP methods (GET, POST, PUT, PATCH, DELETE, OPTIONS)

### Allowed Headers
- All headers

### Credentials
- Cookies and authentication headers allowed

## Path Transformation

Some routes transform paths when proxying:

| Gateway Path | Backend Path | Reason |
|-------------|--------------|--------|
| `/sales/graphql/**` | `/graphql/**` | Namespace separation |
| `/dashboard/graphql/**` | `/graphql/**` | Namespace separation |

This prevents GraphQL endpoint conflicts between services.

## Development

### Prerequisites
- .NET 8 SDK
- Access to all backend microservices
- Docker & Docker Compose (for containerized setup)

### Running Locally
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run
```

### Running with Docker
```bash
# Build image
docker build -t erp-gateway:latest .

# Run container
docker run -p 8080:8080 \
  -e Jwt__Secret=your-secret \
  -e Services__UserManagement=http://user-management:8080 \
  erp-gateway:latest
```

## Usage Examples

### Authentication (No Token Required)
```bash
# Login
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

### Authenticated Request
```bash
# Get products (requires token)
curl -X GET http://localhost:8080/api/v1/products \
  -H "Authorization: Bearer {token}"
```

### GraphQL Request
```bash
# Query sales GraphQL
curl -X POST http://localhost:8080/sales/graphql \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "{ getAllOrders { id customerId totalAmount status } }"
  }'
```

### SignalR Connection (JavaScript)
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:8080/dashboardHub", {
    accessTokenFactory: () => yourJwtToken
  })
  .build();

await connection.start();
```

## Monitoring

### Prometheus Metrics
Available at `/metrics`:
- `http_requests_total` - Total proxied requests
- `http_request_duration_seconds` - Request duration
- `yarp_proxy_*` - YARP-specific metrics

### Request Logging
All requests logged with:
- Request method and path
- Response status code
- Duration
- Backend service
- Client IP (when available)

## Security Features

1. **JWT Validation**: All tokens validated before proxying
2. **Rate Limiting**: Prevents abuse and DDoS
3. **CORS**: Restricts cross-origin requests to known origins
4. **Health Checks**: Prevents routing to unhealthy services
5. **Structured Logging**: Audit trail of all requests
6. **Header Forwarding**: Preserves authentication context

## Error Handling

| Status Code | Meaning | Cause |
|-------------|---------|-------|
| 401 | Unauthorized | Missing or invalid JWT token |
| 429 | Too Many Requests | Rate limit exceeded |
| 502 | Bad Gateway | Backend service unavailable |
| 503 | Service Unavailable | All backend instances unhealthy |
| 504 | Gateway Timeout | Backend service timeout |

## Performance Considerations

1. **Keep-Alive Connections**: Reuses connections to backend services
2. **Async Processing**: All I/O operations are asynchronous
3. **Memory Caching**: Rate limiting uses in-memory cache
4. **Health Check Caching**: Reduces load on backend services
5. **Connection Pooling**: HTTP client connection pooling

## Scalability

### Horizontal Scaling
- Gateway is stateless (except rate limiting in-memory cache)
- Can run multiple instances behind load balancer
- For production, use distributed rate limiting (Redis)

### Load Balancing
- YARP supports multiple destination instances per cluster
- Round-robin load balancing by default
- Automatic removal of unhealthy instances

### Backend Service Discovery
- Currently uses static configuration
- Can be extended with Consul, Eureka, or Kubernetes service discovery

## Production Recommendations

1. **Distributed Rate Limiting**: Use Redis for shared rate limit state
2. **Circuit Breakers**: Enable Polly circuit breaker policies
3. **TLS/HTTPS**: Terminate TLS at gateway
4. **API Keys**: Add API key validation for external clients
5. **Request Size Limits**: Configure max request body size
6. **Timeout Configuration**: Set appropriate timeouts per route
7. **Logging**: Send logs to centralized logging (ELK, Loki)
8. **Metrics**: Export Prometheus metrics to monitoring system
9. **Secrets Management**: Use Azure Key Vault or Kubernetes secrets
10. **Rate Limit Storage**: Use Redis for distributed rate limiting

## Troubleshooting

### Backend Service Not Reachable
- Check health endpoints: `/health/ready` on backend services
- Verify service addresses in configuration
- Check network connectivity between containers

### Authentication Failures
- Verify JWT secret matches user management service
- Check token expiration
- Ensure `Authorization: Bearer {token}` header format

### Rate Limit Issues
- Check IP address detection (`X-Real-IP` header)
- Verify rate limit configuration
- Use localhost exemption for development

### CORS Errors
- Verify origin in CORS policy
- Check for preflight OPTIONS requests
- Ensure credentials flag matches between client and server

## License
Part of the ERP Demo Application - MIT License
