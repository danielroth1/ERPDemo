# ERP System Implementation Guide

This guide provides detailed instructions for implementing each component of the ERP system.

## 🎯 Implementation Order

Follow this order to build the system incrementally:

1. ✅ **Project Structure** - COMPLETED
2. **User Management Service** - IN PROGRESS
3. **MongoDB & Kafka Infrastructure**
4. **Inventory Management Service**
5. **Sales & Orders Service**
6. **Financial Management Service**
7. **Dashboard & Analytics Service**
8. **API Gateway**
9. **React Frontend**
10. **Kubernetes Deployment**
11. **Monitoring Stack**
12. **CI/CD Pipeline**

---

## 1. User Management Service

### Required NuGet Packages
```bash
cd services/user-management/UserManagement
dotnet add package MongoDB.Driver
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Confluent.Kafka
dotnet add package Serilog.AspNetCore
dotnet add package Swashbuckle.AspNetCore
dotnet add package prometheus-net.AspNetCore
dotnet add package AspNetCore.HealthChecks.MongoDb
```

### Project Structure
```
services/user-management/UserManagement/
├── Controllers/
│   ├── AuthController.cs       # Login, Register, Refresh Token
│   └── UsersController.cs      # User CRUD operations
├── Models/
│   ├── User.cs                 # User entity with MongoDB attributes
│   ├── Role.cs                 # Role enumeration
│   ├── RefreshToken.cs         # Refresh token entity
│   └── DTOs/
│       ├── LoginRequest.cs
│       ├── RegisterRequest.cs
│       ├── ChangePasswordRequest.cs
│       └── UserResponse.cs
├── Services/
│   ├── IUserService.cs         # User management interface
│   ├── UserService.cs          # User management implementation
│   ├── IJwtService.cs          # JWT token generation interface
│   ├── JwtService.cs           # JWT token generation implementation
│   ├── IEmailService.cs        # Email sending interface
│   └── EmailService.cs         # SMTP email implementation
├── Infrastructure/
│   ├── MongoDbContext.cs       # MongoDB connection and collections
│   └── KafkaProducer.cs        # Kafka event producer
├── Configuration/
│   ├── MongoDbSettings.cs      # MongoDB configuration
│   ├── JwtSettings.cs          # JWT configuration
│   ├── SmtpSettings.cs         # SMTP configuration
│   └── KafkaSettings.cs        # Kafka configuration
└── Program.cs                   # Service registration and middleware

tests/unit/UserManagement.Tests/
├── Services/
│   ├── UserServiceTests.cs
│   └── JwtServiceTests.cs
└── Controllers/
    └── AuthControllerTests.cs

tests/integration/UserManagement.Integration.Tests/
└── AuthenticationFlowTests.cs
```

### Key Implementation Files

#### Models/User.cs
```csharp
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UserManagement.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("roles")]
    public List<Role> Roles { get; set; } = new() { Role.User };

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}

public enum Role
{
    User,
    Manager,
    Admin
}
```

#### Configuration/MongoDbSettings.cs
```csharp
namespace UserManagement.Configuration;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
```

#### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "MongoDB": {
    "ConnectionString": "mongodb://admin:admin123@mongodb:27017",
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
    "Username": "",
    "Password": "",
    "FromEmail": "noreply@erp-system.com",
    "EnableSsl": true
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "Topic": "user-events"
  }
}
```

#### Program.cs Structure
```csharp
using UserManagement.Configuration;
using UserManagement.Infrastructure;
using UserManagement.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configure MongoDB
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<MongoDbContext>();

// Configure JWT
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<KafkaProducer>();

// Health checks
builder.Services.AddHealthChecks()
    .AddMongoDb(builder.Configuration.GetValue<string>("MongoDB:ConnectionString")!);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpMetrics();
app.MapMetrics();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 2. Inventory Management Service

### Structure
```
services/inventory/Inventory/
├── Controllers/
│   ├── ProductsController.cs
│   └── WarehousesController.cs
├── Models/
│   ├── Product.cs
│   ├── StockAdjustment.cs
│   └── Warehouse.cs
├── Services/
│   ├── ProductService.cs
│   └── StockService.cs
└── Program.cs
```

### Key Features
- Product CRUD with MongoDB
- Stock level tracking
- Low-stock alerts
- Kafka events: ProductCreated, ProductUpdated, StockChanged
- REST API with versioning (/api/v1/products)
- Swagger documentation

---

## 3. Sales & Orders Service

### Structure
```
services/sales/Sales/
├── Controllers/
│   ├── OrdersController.cs (REST)
│   ├── CustomersController.cs (REST)
│   └── GraphQL/
│       └── OrderQueries.cs
├── Models/
│   ├── Order.cs
│   ├── Customer.cs
│   └── Invoice.cs
├── Services/
│   ├── OrderService.cs
│   ├── InvoiceService.cs
│   └── OrderStateMachine.cs
└── Program.cs
```

### Key Features
- Order management with workflow
- Invoice generation
- GraphQL queries for complex data
- Kafka consumer (inventory-events)
- Kafka producer (order-events)

---

## 4. Frontend Implementation

### Initialize React Project
```bash
cd frontend
npm create vite@latest . -- --template react-ts
npm install @reduxjs/toolkit react-redux react-router-dom
npm install axios @apollo/client graphql
npm install tailwindcss postcss autoprefixer
npm install @microsoft/signalr
npm install recharts
npm install react-hook-form
npm install -D @playwright/test vitest @testing-library/react
```

### Project Structure
```
frontend/
├── src/
│   ├── store/
│   │   ├── index.ts
│   │   └── slices/
│   │       ├── authSlice.ts
│   │       ├── inventorySlice.ts
│   │       └── ordersSlice.ts
│   ├── features/
│   │   ├── auth/
│   │   ├── inventory/
│   │   ├── sales/
│   │   ├── financial/
│   │   └── dashboard/
│   ├── components/
│   ├── services/
│   │   ├── api.ts
│   │   └── graphql.ts
│   ├── App.tsx
│   └── main.tsx
└── nginx.conf
```

---

## 5. Kubernetes Deployment

### MongoDB StatefulSet
```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: mongodb
spec:
  serviceName: mongodb
  replicas: 1
  selector:
    matchLabels:
      app: mongodb
  template:
    metadata:
      labels:
        app: mongodb
    spec:
      containers:
      - name: mongodb
        image: mongo:7
        ports:
        - containerPort: 27017
        env:
        - name: MONGO_INITDB_ROOT_USERNAME
          valueFrom:
            secretKeyRef:
              name: mongodb-secret
              key: username
        - name: MONGO_INITDB_ROOT_PASSWORD
          valueFrom:
            secretKeyRef:
              name: mongodb-secret
              key: password
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "768Mi"
            cpu: "500m"
        volumeMounts:
        - name: mongodb-data
          mountPath: /data/db
        livenessProbe:
          exec:
            command:
            - mongo
            - --eval
            - "db.adminCommand('ping')"
          initialDelaySeconds: 30
          periodSeconds: 10
  volumeClaimTemplates:
  - metadata:
      name: mongodb-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      resources:
        requests:
          storage: 10Gi
```

---

## 6. Monitoring Setup

### Prometheus Configuration
```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'user-management'
    static_configs:
      - targets: ['user-management:8080']
  
  - job_name: 'inventory'
    static_configs:
      - targets: ['inventory:8080']
  
  - job_name: 'linkerd-proxies'
    kubernetes_sd_configs:
      - role: pod
    relabel_configs:
      - source_labels: [__meta_kubernetes_pod_container_port_name]
        action: keep
        regex: linkerd-admin
```

---

## 7. CI/CD Pipeline

### GitHub Actions Workflow
```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test-backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Test with coverage
        run: dotnet test --no-build --collect:"XPlat Code Coverage"
      
      - name: Upload coverage
        uses: codecov/codecov-action@v3

  test-frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '20'
      
      - name: Install dependencies
        run: cd frontend && npm ci
      
      - name: Run tests
        run: cd frontend && npm test
      
      - name: E2E tests
        run: cd frontend && npx playwright test

  build-push-images:
    needs: [test-backend, test-frontend]
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Login to Docker Hub
        uses: docker/login-action@v2
        with:
          username: ${{ secrets.DOCKER_USERNAME }}
          password: ${{ secrets.DOCKER_PASSWORD }}
      
      - name: Build and push images
        run: |
          docker build -t ${{ secrets.DOCKER_USERNAME }}/erp-user-management:${{ github.sha }} ./services/user-management
          docker push ${{ secrets.DOCKER_USERNAME }}/erp-user-management:${{ github.sha }}
      
      - name: Scan images
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: ${{ secrets.DOCKER_USERNAME }}/erp-user-management:${{ github.sha }}

  deploy-production:
    needs: build-push-images
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    steps:
      - name: Deploy to server
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.SSH_HOST }}
          username: ${{ secrets.SSH_USER }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          script: |
            kubectl set image deployment/user-management user-management=${{ secrets.DOCKER_USERNAME }}/erp-user-management:${{ github.sha }}
            kubectl rollout status deployment/user-management
```

---

## 8. Testing Strategy

### Unit Tests (xUnit)
```csharp
using Xunit;
using Moq;
using UserManagement.Services;
using UserManagement.Models;

public class UserServiceTests
{
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsUser()
    {
        // Arrange
        var mockRepo = new Mock<IUserRepository>();
        var service = new UserService(mockRepo.Object);
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var result = await service.CreateUserAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Email, result.Email);
    }
}
```

### Integration Tests (TestContainers)
```csharp
using Testcontainers.MongoDb;
using Xunit;

public class AuthenticationFlowTests : IAsyncLifetime
{
    private MongoDbContainer _mongoContainer;

    public async Task InitializeAsync()
    {
        _mongoContainer = new MongoDbBuilder().Build();
        await _mongoContainer.StartAsync();
    }

    [Fact]
    public async Task LoginFlow_WithValidCredentials_ReturnsToken()
    {
        // Test implementation
    }

    public async Task DisposeAsync()
    {
        await _mongoContainer.DisposeAsync();
    }
}
```

### E2E Tests (Playwright)
```typescript
import { test, expect } from '@playwright/test';

test('user login flow', async ({ page }) => {
  await page.goto('http://localhost:3000/login');
  
  await page.fill('[name="email"]', 'admin@example.com');
  await page.fill('[name="password"]', 'Password123!');
  await page.click('button[type="submit"]');
  
  await expect(page).toHaveURL('http://localhost:3000/dashboard');
  await expect(page.locator('text=Welcome')).toBeVisible();
});
```

---

## 9. Local Development Workflow

### Using Skaffold
```bash
# Start development mode with hot reload
skaffold dev --profile=local

# Run tests
skaffold run --profile=testing

# Build all images
skaffold build

# Clean up
skaffold delete
```

### Using Docker Compose (without Kubernetes)
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f user-management

# Stop all services
docker-compose down
```

---

## 10. Production Deployment

### Server Preparation
```bash
# Install K3s
curl -sfL https://get.k3s.io | sh -

# Install kubectl (if not included)
curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
chmod +x kubectl
sudo mv kubectl /usr/local/bin/

# Install cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.crds.yaml
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Install Linkerd
curl -fsL https://run.linkerd.io/install | sh
linkerd check --pre
linkerd install --crds | kubectl apply -f -
linkerd install | kubectl apply -f -
linkerd check
```

### Deploy Application
```bash
# Create namespace
kubectl create namespace erp-prod
kubectl annotate namespace erp-prod linkerd.io/inject=enabled

# Create secrets
kubectl create secret generic mongodb-secret \
  --from-literal=username=admin \
  --from-literal=password=your-password \
  -n erp-prod

kubectl create secret generic jwt-secret \
  --from-literal=secret=your-jwt-secret \
  -n erp-prod

# Apply manifests
kubectl apply -k infrastructure/k8s/production/

# Check deployment
kubectl get pods -n erp-prod
kubectl get ingress -n erp-prod
```

---

## Next Steps

1. **Implement User Management Service** using the structure above
2. **Test locally** with docker-compose
3. **Create remaining services** following similar patterns
4. **Build frontend** with Redux and React
5. **Deploy to Kubernetes** using Skaffold
6. **Set up monitoring** with Grafana dashboards
7. **Configure CI/CD** pipeline
8. **Deploy to production** server

Refer to specific documentation files in `docs/` folder for detailed implementation of each component.
