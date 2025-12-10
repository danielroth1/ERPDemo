# Sales Management Service

ERP Sales & Orders Management microservice with REST API and GraphQL support.

## Features

- **Order Management**: Create, update, and track sales orders
- **Customer Management**: Maintain customer database with contact information
- **Invoice Generation**: Automated invoice creation from orders
- **Payment Tracking**: Record and track invoice payments
- **GraphQL API**: Flexible querying with Hot Chocolate
- **REST API**: Traditional RESTful endpoints
- **Event-Driven**: Kafka integration for sales events
- **Role-Based Authorization**: User, Manager, and Admin roles
- **Health Checks**: MongoDB connectivity monitoring
- **Prometheus Metrics**: Performance and business metrics

## Technology Stack

- **.NET 8**: Modern C# web API
- **MongoDB**: Document database for order/customer/invoice storage
- **Apache Kafka**: Event streaming for order and invoice events
- **Hot Chocolate**: GraphQL server for flexible queries
- **JWT Authentication**: Secure API access
- **Serilog**: Structured logging with JSON formatting
- **Prometheus**: Metrics collection
- **Swagger/OpenAPI**: API documentation

## Architecture

### Models

#### Order
- Customer reference
- Order items with product details
- Pricing (subtotal, tax, discount, total)
- Status workflow (Draft → Pending → Confirmed → Processing → Shipped → Delivered → Completed)
- Shipping and billing addresses
- Timestamps (created, updated, completed, cancelled)

#### Customer
- Personal information (name, email, phone)
- Company details
- Tax ID
- Default addresses
- Active/inactive status

#### Invoice
- Order reference
- Invoice numbering
- Due dates and payment tracking
- Status workflow (Draft → Pending → Sent → PartiallyPaid → Paid → Overdue)
- Payment history

### Services

#### OrderService
- CRUD operations for orders
- Status management
- Customer validation
- Order number generation
- Kafka event publishing (OrderCreated, OrderStatusChanged)

#### CustomerService
- CRUD operations for customers
- Email uniqueness validation
- Search functionality (name, email, company)
- Soft delete (deactivation)

#### InvoiceService
- Invoice generation from orders
- Payment recording
- Overdue tracking
- Invoice number generation
- Kafka event publishing (InvoiceCreated, InvoicePaid)

## API Endpoints

### REST API (api/v1)

#### Orders
- `POST /orders` - Create order (User, Manager, Admin)
- `GET /orders/{id}` - Get order by ID
- `GET /orders` - List all orders (pagination)
- `GET /orders/customer/{customerId}` - List customer orders
- `GET /orders/status/{status}` - Filter by status
- `PUT /orders/{id}` - Update order (Manager, Admin)
- `PATCH /orders/{id}/status` - Update status (Manager, Admin)
- `DELETE /orders/{id}` - Delete draft order (Admin)

#### Customers
- `POST /customers` - Create customer (User, Manager, Admin)
- `GET /customers/{id}` - Get customer by ID
- `GET /customers/email/{email}` - Get by email
- `GET /customers` - List all customers (pagination)
- `GET /customers/search?q={term}` - Search customers
- `PUT /customers/{id}` - Update customer (Manager, Admin)
- `DELETE /customers/{id}` - Deactivate customer (Admin)

#### Invoices
- `POST /invoices` - Create invoice (Manager, Admin)
- `GET /invoices/{id}` - Get invoice by ID
- `GET /invoices/order/{orderId}` - Get by order
- `GET /invoices` - List all invoices (pagination)
- `GET /invoices/customer/{customerId}` - List customer invoices
- `GET /invoices/status/{status}` - Filter by status
- `GET /invoices/overdue` - List overdue invoices
- `PUT /invoices/{id}` - Update invoice (Manager, Admin)
- `POST /invoices/{id}/payments` - Record payment (Manager, Admin)
- `DELETE /invoices/{id}` - Delete draft invoice (Admin)

### GraphQL API (/graphql)

#### Queries
```graphql
query {
  getOrder(id: "order-id") { id orderNumber total status }
  getOrders(skip: 0, limit: 10) { id orderNumber customerId total }
  getOrdersByCustomer(customerId: "cust-id") { id orderNumber }
  
  getCustomer(id: "customer-id") { id firstName lastName email }
  getCustomers(skip: 0, limit: 10) { id firstName lastName }
  searchCustomers(searchTerm: "john") { id firstName lastName email }
  
  getInvoice(id: "invoice-id") { id invoiceNumber total status }
  getInvoices(skip: 0, limit: 10) { id invoiceNumber total }
  getOverdueInvoices { id invoiceNumber dueDate amountDue }
}
```

#### Mutations
```graphql
mutation {
  createOrder(input: { customerId: "cust-id", items: [...] }) { id orderNumber }
  updateOrder(id: "order-id", input: { discount: 10 }) { id total }
  updateOrderStatus(id: "order-id", status: CONFIRMED) { id status }
  deleteOrder(id: "order-id")
  
  createCustomer(input: { firstName: "John", lastName: "Doe", email: "john@example.com" }) { id }
  updateCustomer(id: "customer-id", input: { phone: "+1234567890" }) { id }
  deleteCustomer(id: "customer-id")
  
  createInvoice(input: { orderId: "order-id", dueDate: "2025-01-31" }) { id invoiceNumber }
  recordPayment(id: "invoice-id", input: { amount: 100 }) { id amountPaid status }
  deleteInvoice(id: "invoice-id")
}
```

## Kafka Events

### Published Events (sales-events topic)

#### OrderCreatedEvent
```json
{
  "orderId": "order-id",
  "customerId": "customer-id",
  "orderNumber": "ORD-20250103-000001",
  "total": 1100.00,
  "items": [
    {
      "productId": "product-id",
      "quantity": 10,
      "unitPrice": 100.00
    }
  ],
  "createdAt": "2025-01-03T10:00:00Z"
}
```

#### OrderStatusChangedEvent
```json
{
  "orderId": "order-id",
  "orderNumber": "ORD-20250103-000001",
  "oldStatus": "Pending",
  "newStatus": "Confirmed",
  "changedAt": "2025-01-03T11:00:00Z"
}
```

#### InvoiceCreatedEvent
```json
{
  "invoiceId": "invoice-id",
  "invoiceNumber": "INV-20250103-000001",
  "orderId": "order-id",
  "customerId": "customer-id",
  "total": 1100.00,
  "dueDate": "2025-02-02T00:00:00Z",
  "createdAt": "2025-01-03T10:30:00Z"
}
```

#### InvoicePaidEvent
```json
{
  "invoiceId": "invoice-id",
  "invoiceNumber": "INV-20250103-000001",
  "amountPaid": 1100.00,
  "paidAt": "2025-01-25T14:00:00Z"
}
```

## Configuration

### appsettings.json

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://mongodb:27017",
    "DatabaseName": "erp_sales"
  },
  "Jwt": {
    "Secret": "your-secret-key",
    "Issuer": "erp-sales-service",
    "Audience": "erp-clients"
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "ConsumerGroupId": "sales-service-group"
  }
}
```

## MongoDB Collections

### orders
```javascript
{
  "_id": "order-id",
  "customerId": "customer-id",
  "orderNumber": "ORD-20250103-000001",
  "items": [
    {
      "productId": "product-id",
      "productName": "Product Name",
      "sku": "SKU-123",
      "quantity": 10,
      "unitPrice": 100.00,
      "discount": 0,
      "subtotal": 1000.00,
      "total": 1000.00
    }
  ],
  "subtotal": 1000.00,
  "tax": 100.00,
  "discount": 0,
  "total": 1100.00,
  "status": "Pending",
  "notes": "Customer notes",
  "shippingAddress": {...},
  "billingAddress": {...},
  "createdAt": ISODate("2025-01-03T10:00:00Z"),
  "updatedAt": ISODate("2025-01-03T10:00:00Z"),
  "completedAt": null,
  "cancelledAt": null
}
```

### customers
```javascript
{
  "_id": "customer-id",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "phone": "+1234567890",
  "company": "Acme Corp",
  "taxId": "TAX-123456",
  "defaultBillingAddress": {...},
  "defaultShippingAddress": {...},
  "notes": "VIP customer",
  "isActive": true,
  "createdAt": ISODate("2025-01-01T00:00:00Z"),
  "updatedAt": ISODate("2025-01-01T00:00:00Z")
}
```

### invoices
```javascript
{
  "_id": "invoice-id",
  "invoiceNumber": "INV-20250103-000001",
  "orderId": "order-id",
  "customerId": "customer-id",
  "issueDate": ISODate("2025-01-03T10:30:00Z"),
  "dueDate": ISODate("2025-02-02T00:00:00Z"),
  "subtotal": 1000.00,
  "tax": 100.00,
  "discount": 0,
  "total": 1100.00,
  "amountPaid": 0,
  "amountDue": 1100.00,
  "status": "Pending",
  "items": [...],
  "billingAddress": {...},
  "notes": "Payment terms: Net 30",
  "paidAt": null,
  "createdAt": ISODate("2025-01-03T10:30:00Z"),
  "updatedAt": ISODate("2025-01-03T10:30:00Z")
}
```

## Development

### Prerequisites
- .NET 8 SDK
- MongoDB
- Apache Kafka
- Docker (optional)

### Running Locally

```bash
# Navigate to project directory
cd services/sales/SalesManagement

# Restore dependencies
dotnet restore

# Run the service
dotnet run
```

Service will be available at `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`
- GraphQL UI: `http://localhost:5000/graphql`
- Health Check: `http://localhost:5000/health`
- Metrics: `http://localhost:5000/metrics`

### Running with Docker

```bash
# Build image
docker build -t erp-sales:latest .

# Run container
docker run -p 8080:8080 \
  -e MongoDb__ConnectionString=mongodb://host.docker.internal:27017 \
  -e Kafka__BootstrapServers=host.docker.internal:9092 \
  erp-sales:latest
```

## Testing

### Create a Customer
```bash
curl -X POST http://localhost:5000/api/v1/customers \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "phone": "+1234567890"
  }'
```

### Create an Order
```bash
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "customer-id",
    "items": [
      {
        "productId": "product-id",
        "quantity": 10,
        "discount": 0
      }
    ],
    "discount": 0
  }'
```

### Create an Invoice
```bash
curl -X POST http://localhost:5000/api/v1/invoices \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "order-id",
    "dueDate": "2025-02-02T00:00:00Z"
  }'
```

### Record Payment
```bash
curl -X POST http://localhost:5000/api/v1/invoices/{id}/payments \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 1100.00,
    "paymentDate": "2025-01-15T00:00:00Z"
  }'
```

## Monitoring

### Prometheus Metrics
- HTTP request duration and count
- MongoDB connection health
- Kafka producer metrics
- Order creation rate
- Invoice payment rate

### Health Checks
- MongoDB connectivity
- Application readiness

### Logging
- Structured JSON logs with Serilog
- Request/response logging
- Error tracking with context

## Security

- **JWT Authentication**: Bearer token required for all endpoints
- **Role-Based Authorization**:
  - User: Can view data and create orders/customers
  - Manager: Can create/update orders, customers, invoices
  - Admin: Full access including deletions
- **HTTPS**: Use HTTPS in production
- **Secret Management**: Store JWT secret in environment variables or secret manager

## Future Enhancements

- [ ] PDF invoice generation
- [ ] Email notifications for invoices
- [ ] Recurring orders
- [ ] Order templates
- [ ] Credit notes and refunds
- [ ] Multi-currency support
- [ ] Sales reports and analytics
- [ ] Integration with shipping providers
- [ ] Payment gateway integration
- [ ] Customer portal

## License

Proprietary - Internal ERP System
