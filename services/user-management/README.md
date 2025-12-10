# User Management Service

Authentication and user management microservice for the ERP system.

## Features

- User registration and login
- JWT-based authentication with refresh tokens
- Role-based authorization (User, Manager, Admin)
- Password management (change password, forgot password)
- Email notifications (welcome, password reset, password changed)
- User CRUD operations (Admin only)
- MongoDB persistence
- Kafka event publishing
- Health checks
- Prometheus metrics
- Swagger API documentation

## API Endpoints

### Authentication

- `POST /api/v1/auth/register` - Register new user
- `POST /api/v1/auth/login` - Login user
- `POST /api/v1/auth/refresh` - Refresh access token
- `POST /api/v1/auth/logout` - Logout user (requires authentication)
- `GET /api/v1/auth/me` - Get current user profile (requires authentication)
- `POST /api/v1/auth/change-password` - Change password (requires authentication)
- `POST /api/v1/auth/forgot-password` - Request password reset

### Users (Admin/Manager only)

- `GET /api/v1/users` - Get all users (paginated)
- `GET /api/v1/users/{id}` - Get user by ID
- `PUT /api/v1/users/{id}` - Update user (Admin only)
- `DELETE /api/v1/users/{id}` - Delete user (Admin only)
- `POST /api/v1/users/{id}/deactivate` - Deactivate user (Admin only)

### Health Checks

- `GET /health/live` - Liveness probe (doesn't check dependencies)
- `GET /health/ready` - Readiness probe (checks MongoDB connection)

### Monitoring

- `GET /metrics` - Prometheus metrics endpoint

## Configuration

### Environment Variables

Required configuration in `appsettings.json` or environment variables:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:password@mongodb-service:27017",
    "DatabaseName": "erp_users"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-characters-long",
    "Issuer": "erp-system",
    "Audience": "erp-clients",
    "ExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@erp-system.com",
    "EnableSsl": true
  },
  "Kafka": {
    "BootstrapServers": "kafka-service:9092",
    "Topic": "user-events"
  }
}
```

## MongoDB Collections

### users
- `_id` (ObjectId)
- `email` (string, unique)
- `passwordHash` (string)
- `firstName` (string)
- `lastName` (string)
- `roles` (array of Role enum)
- `isActive` (boolean)
- `emailConfirmed` (boolean)
- `createdAt` (DateTime)
- `updatedAt` (DateTime)
- `lastLoginAt` (DateTime?)

### refreshTokens
- `_id` (ObjectId)
- `userId` (string)
- `token` (string)
- `expiresAt` (DateTime)
- `createdAt` (DateTime)
- `revokedAt` (DateTime?)
- `replacedByToken` (string?)

## Kafka Events

Published events:
- `UserCreated` - When a new user registers
- `UserUpdated` - When user profile is updated
- `UserDeleted` - When a user is deleted
- `UserDeactivated` - When a user is deactivated

Event format:
```json
{
  "EventType": "UserCreated",
  "Timestamp": "2025-12-03T10:00:00Z",
  "Data": {
    "Id": "507f1f77bcf86cd799439011",
    "Email": "user@example.com",
    "FirstName": "John",
    "LastName": "Doe",
    "Roles": ["User"]
  }
}
```

## Local Development

### Prerequisites
- .NET 8 SDK
- MongoDB running on `mongodb://localhost:27017`
- Kafka running on `localhost:9092`

### Run Locally

```bash
cd services/user-management/UserManagement
dotnet restore
dotnet run
```

The service will start on `http://localhost:5000` (HTTP) and `https://localhost:5001` (HTTPS).

Access Swagger UI at: `https://localhost:5001/swagger`

### Docker

Build image:
```bash
cd services/user-management
docker build -t erp-user-management:latest .
```

Run container:
```bash
docker run -p 8080:8080 \
  -e MongoDB__ConnectionString=mongodb://host.docker.internal:27017 \
  -e MongoDB__DatabaseName=erp_users \
  -e Jwt__Secret=your-secret-key-min-32-characters-long \
  erp-user-management:latest
```

## Testing

### Unit Tests
```bash
cd tests/unit
dotnet test UserManagement.UnitTests.csproj
```

### Integration Tests
```bash
cd tests/integration
dotnet test UserManagement.IntegrationTests.csproj
```

## Security Considerations

- Passwords are hashed using BCrypt with salt
- JWT tokens have configurable expiration
- Refresh tokens are stored in database and can be revoked
- All authentication endpoints use HTTPS in production
- Role-based authorization for sensitive operations
- MongoDB credentials should be stored securely (Kubernetes secrets)
- JWT secret must be at least 32 characters and stored securely

## Monitoring & Observability

### Prometheus Metrics
- HTTP request duration and count
- MongoDB connection status
- Process CPU and memory usage
- GC metrics

### Logging
- Structured logging with Serilog
- JSON format for easy parsing
- Request/response logging
- Error tracking with stack traces

### Health Checks
- `/health/live` - Basic liveness check
- `/health/ready` - Checks MongoDB connectivity

## Performance

- Connection pooling for MongoDB
- Async/await for all I/O operations
- Efficient password hashing with BCrypt
- JWT token validation caching
- Kafka producer reuse

## License

Proprietary - Internal ERP System
