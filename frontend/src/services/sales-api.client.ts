// Sales API Client using Kiota-generated client
import { createSalesClient } from '../generated/clients/sales/salesClient';
import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { BearerTokenAuthenticationProvider } from './auth/bearer-token-provider';
import type {
  OrderResponse,
  CreateOrderRequest,
  UpdateOrderRequest,
  UpdateOrderStatusRequest,
  CustomerResponse,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  InvoiceResponse,
  CreateInvoiceRequest,
  RecordPaymentRequest,
} from '../generated/clients/sales/models';

const API_BASE_URL = import.meta.env.VITE_API_GATEWAY_URL || 'http://localhost:5000';

class SalesApiClient {
  private client;

  constructor() {
    const authProvider = new BearerTokenAuthenticationProvider();
    const adapter = new FetchRequestAdapter(authProvider);
    adapter.baseUrl = API_BASE_URL;

    this.client = createSalesClient(adapter);
  }

  // Orders
  async getOrders(skip: number = 0, limit: number = 10): Promise<OrderResponse[]> {
    const response = await this.client.api.v1.orders.get({
      queryParameters: { skip, limit },
    });
    return response?.data || [];
  }

  async getOrder(id: string): Promise<OrderResponse> {
    const response = await this.client.api.v1.orders.byId(id).get();
    if (!response?.data) {
      throw new Error(`Order ${id} not found`);
    }
    return response.data;
  }

  async createOrder(request: CreateOrderRequest): Promise<OrderResponse> {
    const response = await this.client.api.v1.orders.post(request);
    if (!response?.data) {
      throw new Error('Failed to create order');
    }
    return response.data;
  }

  async updateOrder(id: string, request: UpdateOrderRequest): Promise<OrderResponse> {
    const response = await this.client.api.v1.orders.byId(id).put(request);
    if (!response?.data) {
      throw new Error('Failed to update order');
    }
    return response.data;
  }

  async deleteOrder(id: string): Promise<void> {
    await this.client.api.v1.orders.byId(id).delete();
  }

  async updateOrderStatus(id: string, status: string): Promise<OrderResponse> {
    const request: UpdateOrderStatusRequest = { status };
    const response = await this.client.api.v1.orders.byId(id).status.patch(request);
    if (!response?.data) {
      throw new Error('Failed to update order status');
    }
    return response.data;
  }

  // Customers
  async getCustomers(): Promise<CustomerResponse[]> {
    const response = await this.client.api.v1.customers.get();
    return response?.data || [];
  }

  async getCustomer(id: string): Promise<CustomerResponse> {
    const response = await this.client.api.v1.customers.byId(id).get();
    if (!response?.data) {
      throw new Error(`Customer ${id} not found`);
    }
    return response.data;
  }

  async createCustomer(request: CreateCustomerRequest): Promise<CustomerResponse> {
    const response = await this.client.api.v1.customers.post(request);
    if (!response?.data) {
      throw new Error('Failed to create customer');
    }
    return response.data;
  }

  async updateCustomer(id: string, request: UpdateCustomerRequest): Promise<CustomerResponse> {
    const response = await this.client.api.v1.customers.byId(id).put(request);
    if (!response?.data) {
      throw new Error('Failed to update customer');
    }
    return response.data;
  }

  async deleteCustomer(id: string): Promise<void> {
    await this.client.api.v1.customers.byId(id).delete();
  }

  // Invoices
  async getInvoices(): Promise<InvoiceResponse[]> {
    const response = await this.client.api.v1.invoices.get();
    return response?.data || [];
  }

  async getInvoice(id: string): Promise<InvoiceResponse> {
    const response = await this.client.api.v1.invoices.byId(id).get();
    if (!response?.data) {
      throw new Error(`Invoice ${id} not found`);
    }
    return response.data;
  }

  async createInvoice(request: CreateInvoiceRequest): Promise<InvoiceResponse> {
    const response = await this.client.api.v1.invoices.post(request);
    if (!response?.data) {
      throw new Error('Failed to create invoice');
    }
    return response.data;
  }

  async recordPayment(invoiceId: string, request: RecordPaymentRequest): Promise<InvoiceResponse> {
    const response = await this.client.api.v1.invoices.byId(invoiceId).payments.post(request);
    if (!response?.data) {
      throw new Error('Failed to record payment');
    }
    return response.data;
  }
}

export const salesApiClient = new SalesApiClient();
export default salesApiClient;
