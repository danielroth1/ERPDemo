import { http, HttpResponse } from 'msw';

// Default mock API handlers — override in individual tests with server.use(...)
export const handlers = [
  // Auth endpoints
  http.post('/api/v1/auth/login', () => {
    return HttpResponse.json({
      success: true,
      data: {
        accessToken: 'mock-access-token',
        refreshToken: 'mock-refresh-token',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        user: {
          id: 'user-1',
          email: 'test@example.com',
          firstName: 'Test',
          lastName: 'User',
          roles: [2],
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z',
        },
      },
    });
  }),

  http.post('/api/v1/auth/register', () => {
    return HttpResponse.json({
      success: true,
      data: {
        accessToken: 'mock-access-token',
        refreshToken: 'mock-refresh-token',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        user: {
          id: 'user-new',
          email: 'new@example.com',
          firstName: 'New',
          lastName: 'User',
          roles: [],
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z',
        },
      },
    });
  }),

  http.post('/api/v1/auth/logout', () => {
    return HttpResponse.json({ success: true });
  }),

  http.post('/api/v1/auth/refresh', () => {
    return HttpResponse.json({
      success: true,
      data: {
        accessToken: 'mock-refreshed-token',
        refreshToken: 'mock-refresh-token-2',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        user: {
          id: 'user-1',
          email: 'test@example.com',
          firstName: 'Test',
          lastName: 'User',
          roles: [2],
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z',
        },
      },
    });
  }),

  // Users endpoints
  http.get('/api/v1/users', () => {
    return HttpResponse.json({
      success: true,
      data: {
        items: [
          {
            id: 'user-1',
            email: 'admin@example.com',
            firstName: 'Admin',
            lastName: 'User',
            roles: [2],
            isActive: true,
            createdAt: '2024-01-01T00:00:00Z',
          },
          {
            id: 'user-2',
            email: 'manager@example.com',
            firstName: 'Manager',
            lastName: 'User',
            roles: [1],
            isActive: true,
            createdAt: '2024-01-02T00:00:00Z',
          },
        ],
        page: 1,
        pageSize: 10,
        totalCount: 2,
        totalPages: 1,
      },
    });
  }),

  // Inventory endpoints
  http.get('/api/v1/products', () => {
    return HttpResponse.json({
      items: [
        {
          id: 'prod-1',
          name: 'Widget A',
          description: 'A test widget',
          sku: 'WGT-001',
          categoryId: 'cat-1',
          unitPrice: 29.99,
          stockQuantity: 100,
          reorderLevel: 10,
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 'prod-2',
          name: 'Widget B',
          description: 'Another test widget',
          sku: 'WGT-002',
          categoryId: 'cat-1',
          unitPrice: 49.99,
          stockQuantity: 50,
          reorderLevel: 5,
          isActive: true,
          createdAt: '2024-01-02T00:00:00Z',
          updatedAt: '2024-01-02T00:00:00Z',
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 2,
      totalPages: 1,
    });
  }),

  http.get('/api/v1/categories', () => {
    return HttpResponse.json([
      { id: 'cat-1', name: 'Electronics', description: 'Electronic items', createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
      { id: 'cat-2', name: 'Office Supplies', description: 'Office items', createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
    ]);
  }),

  http.get('/api/v1/categories/product-count', () => {
    return HttpResponse.json([
      { categoryId: 'cat-1', categoryName: 'Electronics', productCount: 5 },
      { categoryId: 'cat-2', categoryName: 'Office Supplies', productCount: 3 },
    ]);
  }),

  // Sales endpoints
  http.get('/api/v1/orders', () => {
    return HttpResponse.json({
      items: [
        {
          id: 'order-1',
          orderNumber: 'ORD-001',
          customerId: 'cust-1',
          orderDate: '2024-01-15T00:00:00Z',
          status: 'Pending',
          totalAmount: 299.99,
          notes: '',
          items: [],
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    });
  }),

  http.get('/api/v1/customers', () => {
    return HttpResponse.json([
      { id: 'cust-1', name: 'Acme Corp', email: 'contact@acme.com', phone: '555-0100', isActive: true },
    ]);
  }),

  // Financial endpoints
  http.get('/api/v1/accounts', () => {
    return HttpResponse.json([
      { id: 'acc-1', accountNumber: 'ACC-001', name: 'Cash', type: 'Asset', balance: 10000, isActive: true },
      { id: 'acc-2', accountNumber: 'ACC-002', name: 'Revenue', type: 'Revenue', balance: 50000, isActive: true },
    ]);
  }),

  http.get('/api/v1/transactions', () => {
    return HttpResponse.json({
      items: [
        {
          id: 'txn-1',
          transactionNumber: 'TXN-001',
          date: '2024-01-15T00:00:00Z',
          description: 'Sale payment',
          amount: 299.99,
          type: 'Sale',
          debitAccountId: 'acc-1',
          creditAccountId: 'acc-2',
          reference: 'ORD-001',
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    });
  }),

  // Dashboard/Analytics endpoints
  http.get('/api/v1/dashboard/kpis', () => {
    return HttpResponse.json({
      totalRevenue: 150000,
      totalOrders: 250,
      totalProducts: 100,
      totalCustomers: 50,
    });
  }),

  http.get('/api/v1/alerts', () => {
    return HttpResponse.json([]);
  }),

  http.get('/api/v1/alerts/unread', () => {
    return HttpResponse.json([]);
  }),

  http.get('/api/v1/dashboard/summary', () => {
    return HttpResponse.json({
      totalRevenue: 150000,
      totalOrders: 250,
      totalProducts: 100,
      activeUsers: 25,
    });
  }),

  http.get('/api/v1/dashboard/top-products', () => {
    return HttpResponse.json([
      { id: 'prod-1', name: 'Widget A', totalSold: 500, revenue: 14995 },
    ]);
  }),

  http.get('/api/v1/dashboard/metrics', () => {
    return HttpResponse.json({ cpu: 45, memory: 62, uptime: 99.9 });
  }),
];
