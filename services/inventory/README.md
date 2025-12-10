# Inventory Management Service

Product inventory and stock management microservice for the ERP system.

## Features

- Product CRUD operations
- Category management
- Stock tracking and movements
- Low stock alerts
- Stock adjustments
- Search functionality
- MongoDB persistence
- Kafka event publishing
- Health checks
- Prometheus metrics
- Swagger API documentation

## API Endpoints

### Products

- `GET /api/v1/products` - Get all products (paginated)
- `GET /api/v1/products/{id}` - Get product by ID
- `GET /api/v1/products/sku/{sku}` - Get product by SKU
- `GET /api/v1/products/search?q=term` - Search products
- `GET /api/v1/products/low-stock` - Get low stock alerts (Manager/Admin)
- `POST /api/v1/products` - Create product (Manager/Admin)
- `PUT /api/v1/products/{id}` - Update product (Manager/Admin)
- `DELETE /api/v1/products/{id}` - Delete product (Admin)
- `POST /api/v1/products/{id}/adjust-stock` - Adjust stock quantity (Manager/Admin)

### Categories

- `GET /api/v1/categories` - Get all categories
- `GET /api/v1/categories/{id}` - Get category by ID
- `POST /api/v1/categories` - Create category (Manager/Admin)
- `PUT /api/v1/categories/{id}` - Update category (Manager/Admin)
- `DELETE /api/v1/categories/{id}` - Delete category (Admin)

### Stock Movements

- `GET /api/v1/stockmovements/product/{productId}` - Get movements for product
- `GET /api/v1/stockmovements/recent` - Get recent movements (Manager/Admin)
- `POST /api/v1/stockmovements` - Record stock movement (Manager/Admin)

### Health Checks

- `GET /health/live` - Liveness probe
- `GET /health/ready` - Readiness probe (checks MongoDB)

### Monitoring

- `GET /metrics` - Prometheus metrics endpoint

## Configuration

### Environment Variables

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://admin:password@mongodb-service:27017",
    "DatabaseName": "erp_inventory"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-characters-long",
    "Issuer": "erp-system",
    "Audience": "erp-clients"
  },
  "Kafka": {
    "BootstrapServers": "kafka-service:9092",
    "Topic": "inventory-events"
  }
}
```

## MongoDB Collections

### products
- `_id` (ObjectId)
- `sku` (string, unique)
- `name` (string)
- `description` (string)
- `categoryId` (string)
- `price` (decimal)
- `cost` (decimal)
- `stockQuantity` (int)
- `minStockLevel` (int)
- `maxStockLevel` (int)
- `unit` (string)
- `isActive` (boolean)
- `createdAt` (DateTime)
- `updatedAt` (DateTime)

### categories
- `_id` (ObjectId)
- `name` (string)
- `description` (string)
- `isActive` (boolean)
- `createdAt` (DateTime)

### stock_movements
- `_id` (ObjectId)
- `productId` (string)
- `movementType` (enum: Purchase, Sale, Return, Adjustment, Transfer)
- `quantity` (int)
- `reference` (string)
- `notes` (string)
- `createdBy` (string)
- `createdAt` (DateTime)

## Kafka Events

Published events:
- `ProductCreated` - New product added
- `ProductUpdated` - Product details changed
- `ProductDeleted` - Product removed
- `StockUpdated` - Stock quantity changed
- `LowStockAlert` - Stock below minimum level
- `StockMovementCreated` - Stock movement recorded

Event format:
```json
{
  "EventType": "ProductCreated",
  "Timestamp": "2025-12-03T10:00:00Z",
  "Data": {
    "Id": "507f1f77bcf86cd799439011",
    "Sku": "PROD-001",
    "Name": "Sample Product",
    "Price": 99.99,
    "StockQuantity": 100
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
cd services/inventory/InventoryManagement
dotnet restore
dotnet run
```

Access Swagger UI at: `https://localhost:5001/swagger`

### Docker

Build image:
```bash
cd services/inventory
docker build -t erp-inventory:latest .
```

Run container:
```bash
docker run -p 8080:8080 \
  -e MongoDB__ConnectionString=mongodb://host.docker.internal:27017 \
  -e MongoDB__DatabaseName=erp_inventory \
  -e Jwt__Secret=your-secret-key-min-32-characters-long \
  erp-inventory:latest
```

## Authorization

All endpoints require JWT authentication. Role-based access:
- **User**: View products and categories
- **Manager**: Create/update products, adjust stock, view alerts
- **Admin**: All operations including delete

## Business Logic

### Stock Movements
- **Purchase**: Increases stock quantity
- **Sale**: Decreases stock quantity
- **Return**: Increases stock quantity
- **Adjustment**: Sets exact stock quantity
- **Transfer**: For inter-warehouse transfers

### Low Stock Alerts
- Products with stock ≤ minimum level trigger alerts
- Automatically published to Kafka
- Suggested reorder quantity = max level - current stock

### Stock Adjustment
- Used for inventory reconciliation
- Records movement with type "Adjustment"
- Updates product stock to exact quantity

## Performance

- Indexed SKU field for fast lookups
- Pagination on all list endpoints
- Connection pooling for MongoDB
- Async/await for all I/O operations
- Kafka producer reuse

## License

Proprietary - Internal ERP System
