// User types

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: number[];
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
}

export const UserRole = {
  Admin: 'Admin',
  Manager: 'Manager',
  User: 'User',
} as const;

export type UserRole = (typeof UserRole)[keyof typeof UserRole];

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

// Product types
export interface Product {
  id: string;
  name: string;
  description: string;
  sku: string;
  categoryId: string;
  category?: Category;
  unitPrice: number;
  stockQuantity: number;
  reorderLevel: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Category {
  id: string;
  name: string;
  description: string;
  createdAt: string;
  updatedAt: string;
}

export interface StockMovement {
  id: string;
  productId: string;
  product?: Product;
  movementType: MovementType;
  quantity: number;
  reference: string;
  notes: string;
  createdAt: string;
}

export const MovementType = {
  Purchase: 'Purchase',
  Sale: 'Sale',
  Adjustment: 'Adjustment',
  Return: 'Return',
} as const;

export type MovementType = (typeof MovementType)[keyof typeof MovementType];

// Order types
export interface Order {
  id: string;
  orderNumber: string;
  customerId: string;
  customer?: Customer;
  orderDate: string;
  status: OrderStatus;
  totalAmount: number;
  notes: string;
  items: OrderItem[];
  createdAt: string;
  updatedAt: string;
}

export interface OrderItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export const OrderStatus = {
  Pending: 'Pending',
  Confirmed: 'Confirmed',
  Processing: 'Processing',
  Shipped: 'Shipped',
  Delivered: 'Delivered',
  Cancelled: 'Cancelled',
} as const;

export type OrderStatus = (typeof OrderStatus)[keyof typeof OrderStatus];

export interface Customer {
  id: string;
  name: string;
  email: string;
  phone: string;
  address: Address;
  createdAt: string;
  updatedAt: string;
}

export interface Address {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  orderId: string;
  order?: Order;
  issueDate: string;
  dueDate: string;
  totalAmount: number;
  isPaid: boolean;
  paidDate?: string;
  createdAt: string;
}

// Financial types
export interface Account {
  id: string;
  accountNumber: string;
  name: string;
  type: AccountType;
  balance: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export const AccountType = {
  Asset: 'Asset',
  Liability: 'Liability',
  Equity: 'Equity',
  Revenue: 'Revenue',
  Expense: 'Expense',
} as const;

export type AccountType = (typeof AccountType)[keyof typeof AccountType];

export interface Transaction {
  id: string;
  transactionNumber: string;
  date: string;
  description: string;
  amount: number;
  type: TransactionType;
  debitAccountId: string;
  creditAccountId: string;
  reference: string;
  createdAt: string;
}

export const TransactionType = {
  Sale: 'Sale',
  Purchase: 'Purchase',
  Payment: 'Payment',
  Receipt: 'Receipt',
  Transfer: 'Transfer',
  Adjustment: 'Adjustment',
} as const;

export type TransactionType = (typeof TransactionType)[keyof typeof TransactionType];

export interface Budget {
  id: string;
  name: string;
  category: string;
  amount: number;
  period: BudgetPeriod;
  startDate: string;
  endDate: string;
  spent: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export const BudgetPeriod = {
  Monthly: 'Monthly',
  Quarterly: 'Quarterly',
  Yearly: 'Yearly',
} as const;

export type BudgetPeriod = (typeof BudgetPeriod)[keyof typeof BudgetPeriod];

// Dashboard types
export interface DashboardMetrics {
  totalUsers: number;
  totalProducts: number;
  totalOrders: number;
  totalRevenue: number;
  totalExpenses: number;
  lowStockProducts: number;
  pendingOrders: number;
  timestamp: string;
}

export interface KPI {
  id: string;
  name: string;
  description: string;
  targetValue: number;
  currentValue: number;
  previousValue: number;
  percentageChange: number;
  unit: string;
  status: KPIStatus;
  startDate: string;
  endDate: string;
  createdAt: string;
  updatedAt: string;
}

export const KPIStatus = {
  OnTrack: 'OnTrack',
  NeedsAttention: 'NeedsAttention',
  Critical: 'Critical',
} as const;

export type KPIStatus = (typeof KPIStatus)[keyof typeof KPIStatus];

export interface Alert {
  id: string;
  title: string;
  message: string;
  severity: AlertSeverity;
  isRead: boolean;
  source: string;
  createdAt: string;
}

export const AlertSeverity = {
  Info: 'Info',
  Warning: 'Warning',
  Error: 'Error',
  Critical: 'Critical',
} as const;

export type AlertSeverity = (typeof AlertSeverity)[keyof typeof AlertSeverity];

// API response types
export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

// Error types
export interface ApiError {
  message: string;
  errors?: Record<string, string[]>;
  statusCode: number;
}
