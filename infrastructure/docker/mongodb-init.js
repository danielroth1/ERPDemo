// MongoDB initialization script
// This script runs when MongoDB container starts for the first time

db = db.getSiblingDB('admin');

// Create databases
db.getSiblingDB('erp_users');
db.getSiblingDB('erp_inventory');
db.getSiblingDB('erp_sales');
db.getSiblingDB('erp_financial');
db.getSiblingDB('erp_dashboard');

// Create users collection in erp_users database
db = db.getSiblingDB('erp_users');
db.createCollection('users');
db.users.createIndex({ email: 1 }, { unique: true });
db.users.createIndex({ createdAt: 1 });

db.createCollection('refresh_tokens');
db.refresh_tokens.createIndex({ userId: 1 });
db.refresh_tokens.createIndex({ expiresAt: 1 }, { expireAfterSeconds: 0 });

// Create collections in erp_inventory database
db = db.getSiblingDB('erp_inventory');
db.createCollection('products');
db.products.createIndex({ sku: 1 }, { unique: true });
db.products.createIndex({ category: 1 });
db.products.createIndex({ stockLevel: 1 });

db.createCollection('stock_adjustments');
db.stock_adjustments.createIndex({ productId: 1 });
db.stock_adjustments.createIndex({ createdAt: -1 });

db.createCollection('warehouses');

// Create collections in erp_sales database
db = db.getSiblingDB('erp_sales');
db.createCollection('orders');
db.orders.createIndex({ customerId: 1 });
db.orders.createIndex({ status: 1 });
db.orders.createIndex({ createdAt: -1 });

db.createCollection('customers');
db.customers.createIndex({ email: 1 }, { unique: true });

db.createCollection('invoices');
db.invoices.createIndex({ orderId: 1 });

// Create collections in erp_financial database
db = db.getSiblingDB('erp_financial');
db.createCollection('ledger_entries');
db.ledger_entries.createIndex({ date: -1 });
db.ledger_entries.createIndex({ accountType: 1 });

db.createCollection('expenses');
db.expenses.createIndex({ date: -1 });
db.expenses.createIndex({ category: 1 });

db.createCollection('revenue');
db.revenue.createIndex({ date: -1 });

// Create collections in erp_dashboard database
db = db.getSiblingDB('erp_dashboard');
db.createCollection('daily_summary');
db.daily_summary.createIndex({ date: -1 }, { unique: true });

db.createCollection('product_analytics');
db.product_analytics.createIndex({ productId: 1 });

db.createCollection('customer_analytics');
db.customer_analytics.createIndex({ customerId: 1 });

print('MongoDB initialization completed successfully!');
