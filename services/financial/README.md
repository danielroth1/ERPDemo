# Financial Management Service

The Financial Management service handles all accounting operations for the ERP system, implementing proper double-entry bookkeeping, transaction management, budget tracking, and financial reporting.

## Features

### Double-Entry Bookkeeping
- **Balanced Transactions**: Every transaction must have equal debits and credits
- **Journal Entries**: Each transaction contains multiple journal entries
- **Account Balance Updates**: Automatic balance calculations based on account type
- **Transaction Atomicity**: MongoDB transactions ensure all updates succeed or fail together
- **Void Functionality**: Ability to reverse transactions by swapping debits and credits

### Account Management
- **Hierarchical Accounts**: Support for parent-child account relationships
- **Account Types**: Asset, Liability, Equity, Revenue, Expense
- **Account Categories**: 11 predefined categories (Cash, AccountsReceivable, Inventory, etc.)
- **Automatic Numbering**: Type-based account number generation (1xxx for Assets, 2xxx for Liabilities, etc.)
- **Multi-Currency Support**: Track accounts in different currencies
- **Soft Deletion**: Archived accounts remain in system for historical reference

### Budget Management
- **Period-Based Budgets**: Monthly, Quarterly, or Yearly budgets
- **Spending Tracking**: Automatic updates when transactions posted
- **Exceeded Alerts**: Kafka events published when budget exceeded
- **Real-Time Remaining**: Calculate remaining budget based on spent amount
- **Account-Specific**: Budgets tied to specific accounts

### Financial Reporting
- **Balance Sheet**: Assets, Liabilities, and Equity sections with totals
- **Income Statement**: Revenue, Expenses, and Net Income for date range
- **Accounting Equation Validation**: Ensures Assets = Liabilities + Equity
- **Period Analysis**: Aggregate transaction activity within date ranges

## Architecture

### Technologies
- **ASP.NET Core 8**: Web API framework
- **MongoDB 7**: Document database for financial data
- **Apache Kafka**: Event streaming for financial events
- **Prometheus**: Metrics collection
- **Serilog**: Structured logging
- **Swagger/OpenAPI**: API documentation

### Domain Models

#### Account
```csharp
public class Account
{
    public string Id { get; set; }
    public string AccountNumber { get; set; }  // 1001, 2001, etc.
    public string Name { get; set; }
    public AccountType Type { get; set; }      // Asset, Liability, etc.
    public AccountCategory Category { get; set; }  // Cash, Inventory, etc.
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ParentAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### Transaction
```csharp
public class Transaction
{
    public string Id { get; set; }
    public string TransactionNumber { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Description { get; set; }
    public List<JournalEntry> Entries { get; set; }  // Multiple debits/credits
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }  // Draft, Posted, Voided
    public string? ReferenceId { get; set; }  // Link to order, invoice, etc.
    public string CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JournalEntry
{
    public string AccountId { get; set; }
    public string AccountName { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Memo { get; set; }
}
```

#### Budget
```csharp
public class Budget
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string AccountId { get; set; }
    public BudgetPeriod Period { get; set; }  // Monthly, Quarterly, Yearly
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining => Amount - Spent;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
```

### Double-Entry Bookkeeping Logic

The system implements proper accounting principles:

1. **Balance Calculation by Account Type**:
   - **Assets & Expenses**: Increase with debits, decrease with credits
     - `Balance += (Debit - Credit)`
   - **Liabilities, Equity & Revenue**: Increase with credits, decrease with debits
     - `Balance += (Credit - Debit)`

2. **Transaction Validation**:
   - Total debits must equal total credits
   - All referenced accounts must exist
   - Transaction date cannot be in future

3. **Atomicity**:
   - MongoDB transaction sessions ensure multi-document updates
   - If any account balance update fails, entire transaction rolls back
   - Kafka events only published after successful commit

### Example Transactions

#### Sales Transaction
When selling inventory for $1,000:
```json
{
  "description": "Sale of products to Customer ABC",
  "transactionDate": "2024-01-15",
  "type": "Sale",
  "entries": [
    {
      "accountId": "cash-account-id",
      "accountName": "Cash",
      "debit": 1000,
      "credit": 0,
      "memo": "Payment received"
    },
    {
      "accountId": "revenue-account-id",
      "accountName": "Sales Revenue",
      "debit": 0,
      "credit": 1000,
      "memo": "Product sales"
    }
  ]
}
```

#### Expense Transaction
When paying rent of $2,000:
```json
{
  "description": "Monthly rent payment",
  "transactionDate": "2024-01-01",
  "type": "Expense",
  "entries": [
    {
      "accountId": "rent-expense-id",
      "accountName": "Rent Expense",
      "debit": 2000,
      "credit": 0
    },
    {
      "accountId": "cash-account-id",
      "accountName": "Cash",
      "debit": 0,
      "credit": 2000
    }
  ]
}
```

## API Endpoints

### Accounts API (`/api/v1/accounts`)

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/api/v1/accounts` | Create new account | Manager, Admin |
| GET | `/api/v1/accounts/{id}` | Get account by ID | All authenticated |
| GET | `/api/v1/accounts/number/{accountNumber}` | Get by account number | All authenticated |
| GET | `/api/v1/accounts` | List all accounts (paginated) | All authenticated |
| GET | `/api/v1/accounts/type/{type}` | Filter by account type | All authenticated |
| GET | `/api/v1/accounts/{id}/balance` | Get current balance | All authenticated |
| PUT | `/api/v1/accounts/{id}` | Update account | Manager, Admin |
| DELETE | `/api/v1/accounts/{id}` | Soft delete account | Admin |

### Transactions API (`/api/v1/transactions`)

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/api/v1/transactions` | Create transaction | Manager, Admin |
| GET | `/api/v1/transactions/{id}` | Get transaction by ID | All authenticated |
| GET | `/api/v1/transactions` | List all transactions (paginated) | All authenticated |
| GET | `/api/v1/transactions/account/{accountId}` | Filter by account | All authenticated |
| GET | `/api/v1/transactions/date-range` | Filter by date range | All authenticated |
| POST | `/api/v1/transactions/{id}/void` | Void transaction | Admin |

### Budgets API (`/api/v1/budgets`)

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| POST | `/api/v1/budgets` | Create budget | Manager, Admin |
| GET | `/api/v1/budgets/{id}` | Get budget by ID | All authenticated |
| GET | `/api/v1/budgets` | List all budgets (paginated) | All authenticated |
| GET | `/api/v1/budgets/active` | List active budgets | All authenticated |
| GET | `/api/v1/budgets/account/{accountId}` | Filter by account | All authenticated |
| PUT | `/api/v1/budgets/{id}` | Update budget | Manager, Admin |
| DELETE | `/api/v1/budgets/{id}` | Soft delete budget | Admin |

### Reports API (`/api/v1/reports`)

| Method | Endpoint | Description | Authorization |
|--------|----------|-------------|---------------|
| GET | `/api/v1/reports/balance-sheet` | Generate balance sheet | Manager, Admin |
| GET | `/api/v1/reports/income-statement` | Generate income statement | Manager, Admin |

#### Balance Sheet Query Parameters
- `asOfDate` (optional): Date for balance sheet snapshot (defaults to current date)

#### Income Statement Query Parameters
- `startDate` (required): Period start date
- `endDate` (required): Period end date

## Kafka Events

### TransactionCreatedEvent
Published to topic: `financial.transaction.created`

```json
{
  "transactionId": "tx-123",
  "transactionNumber": "TXN-2024-001",
  "transactionDate": "2024-01-15T10:30:00Z",
  "type": "Sale",
  "totalAmount": 1000.00,
  "entries": [
    {
      "accountId": "acc-1",
      "accountName": "Cash",
      "debit": 1000,
      "credit": 0
    },
    {
      "accountId": "acc-2",
      "accountName": "Revenue",
      "debit": 0,
      "credit": 1000
    }
  ],
  "referenceId": "order-456",
  "createdBy": "user-789",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### BudgetExceededEvent
Published to topic: `financial.budget.exceeded`

```json
{
  "budgetId": "budget-123",
  "budgetName": "Marketing Q1 2024",
  "accountId": "acc-marketing",
  "accountName": "Marketing Expense",
  "budgetAmount": 10000.00,
  "spentAmount": 10500.00,
  "exceededAmount": 500.00,
  "percentageUsed": 105.0,
  "period": "Quarterly",
  "startDate": "2024-01-01",
  "endDate": "2024-03-31",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## MongoDB Collections

### accounts
- **Indexes**: 
  - `AccountNumber` (unique)
  - `Type`
  - `IsActive`
  - `ParentAccountId`

### transactions
- **Indexes**: 
  - `TransactionNumber` (unique)
  - `TransactionDate`
  - `Status`
  - `Entries.AccountId`
  - `ReferenceId`

### budgets
- **Indexes**: 
  - `AccountId`
  - `StartDate, EndDate` (compound)
  - `IsActive`

## Configuration

### appsettings.json
```json
{
  "MongoDb": {
    "ConnectionString": "mongodb://mongodb:27017",
    "DatabaseName": "erp_financial"
  },
  "Jwt": {
    "Secret": "your-secret-key",
    "Issuer": "erp-financial-service",
    "Audience": "erp-clients"
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "ConsumerGroupId": "financial-service-group"
  }
}
```

## Development

### Prerequisites
- .NET 8 SDK
- MongoDB 7+
- Apache Kafka 7.5+
- Docker & Docker Compose

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
docker build -t erp-financial:latest .

# Run container
docker run -p 8080:8080 \
  -e MongoDb__ConnectionString=mongodb://mongodb:27017 \
  -e Kafka__BootstrapServers=kafka:9092 \
  erp-financial:latest
```

## Health Checks
- **Liveness**: `GET /health/live` - Basic application health
- **Readiness**: `GET /health/ready` - MongoDB connectivity

## Metrics
Prometheus metrics available at `/metrics`:
- `http_requests_total` - Total HTTP requests
- `http_request_duration_seconds` - Request duration histogram
- Custom business metrics via Prometheus.NET

## API Documentation
Swagger UI available at `/swagger` when running in development mode.

## Security
- **JWT Authentication**: All endpoints require valid JWT token
- **Role-Based Authorization**: Manager and Admin roles for sensitive operations
- **User Context**: CreatedBy fields populated from JWT claims
- **Input Validation**: DTOs with data annotations

## Testing

### Example: Create Account
```bash
curl -X POST http://localhost:8080/api/v1/accounts \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Cash - Main Account",
    "type": "Asset",
    "category": "Cash",
    "currency": "USD"
  }'
```

### Example: Create Transaction
```bash
curl -X POST http://localhost:8080/api/v1/transactions \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Product sale",
    "transactionDate": "2024-01-15T10:00:00Z",
    "type": "Sale",
    "entries": [
      {
        "accountId": "cash-account-id",
        "debit": 1000,
        "credit": 0
      },
      {
        "accountId": "revenue-account-id",
        "debit": 0,
        "credit": 1000
      }
    ]
  }'
```

### Example: Generate Balance Sheet
```bash
curl -X GET "http://localhost:8080/api/v1/reports/balance-sheet?asOfDate=2024-01-31" \
  -H "Authorization: Bearer {token}"
```

## Business Rules

1. **Account Deletion**: Accounts with non-zero balance cannot be deleted
2. **Transaction Posting**: Only Draft transactions can be edited
3. **Void Transactions**: Only Posted transactions can be voided (not Draft or already Voided)
4. **Budget Periods**: Budget dates must not overlap for same account
5. **Account Numbers**: Auto-generated based on type (cannot be manually set)
6. **Balance Consistency**: Account balances are derived from transactions, not manually editable

## Error Handling
- `400 Bad Request`: Validation errors, unbalanced transactions
- `401 Unauthorized`: Missing or invalid JWT token
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Account, transaction, or budget not found
- `500 Internal Server Error`: Database or Kafka connectivity issues

## License
Part of the ERP Demo Application - MIT License
