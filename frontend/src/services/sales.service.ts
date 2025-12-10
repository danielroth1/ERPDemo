import { salesApiClient } from './sales-api.client';
import type { Order, Customer, PaginatedResponse } from '../types';
import type { OrderResponse, CustomerResponse } from '../generated/clients/sales/models';

// Map Kiota types to legacy types
function mapOrderResponse(order: OrderResponse): Order {
  return {
    id: order.id || '',
    orderNumber: order.orderNumber || '',
    customerId: order.customerId || '',
    orderDate: order.createdAt?.toISOString() || new Date().toISOString(),
    status: order.status as any,
    totalAmount: order.total || 0,
    notes: order.notes || '',
    items: [],
    createdAt: order.createdAt?.toISOString() || new Date().toISOString(),
    updatedAt: order.updatedAt?.toISOString() || new Date().toISOString(),
  };
}

function mapCustomerResponse(customer: CustomerResponse): Customer {
  const fullName = `${customer.firstName || ''} ${customer.lastName || ''}`.trim() || customer.company || 'Unknown';
  
  return {
    id: customer.id || '',
    name: fullName,
    email: customer.email || '',
    phone: customer.phone || '',
    address: customer.defaultBillingAddress as any || {
      street: '',
      city: '',
      state: '',
      postalCode: '',
      country: '',
    },
    createdAt: customer.createdAt?.toISOString() || new Date().toISOString(),
    updatedAt: customer.updatedAt?.toISOString() || new Date().toISOString(),
  };
}

class SalesService {
  // Orders
  async getOrders(page: number = 1, pageSize: number = 10): Promise<PaginatedResponse<Order>> {
    const skip = (page - 1) * pageSize;
    const orders = await salesApiClient.getOrders(skip, pageSize);
    
    return {
      items: orders.map(mapOrderResponse),
      page: page,
      pageSize: pageSize,
      totalCount: orders.length,
      totalPages: 1,
    };
  }

  async getOrder(id: string): Promise<Order> {
    const order = await salesApiClient.getOrder(id);
    return mapOrderResponse(order);
  }

  async createOrder(order: Partial<Order>): Promise<Order> {
    const response = await salesApiClient.createOrder({
      customerId: order.customerId || '',
      items: [],
      discount: 0,
      notes: order.notes,
    });
    return mapOrderResponse(response);
  }

  async updateOrder(id: string, order: Partial<Order>): Promise<Order> {
    const response = await salesApiClient.updateOrder(id, {
      notes: order.notes,
    });
    return mapOrderResponse(response);
  }

  async deleteOrder(id: string): Promise<void> {
    await salesApiClient.deleteOrder(id);
  }

  async updateOrderStatus(id: string, status: string): Promise<Order> {
    const response = await salesApiClient.updateOrderStatus(id, status);
    return mapOrderResponse(response);
  }

  // Customers
  async getCustomers(): Promise<Customer[]> {
    const customers = await salesApiClient.getCustomers();
    return customers.map(mapCustomerResponse);
  }

  async createCustomer(customer: Partial<Customer>): Promise<Customer> {
    // Split name into firstName/lastName
    const nameParts = (customer.name || '').split(' ');
    const firstName = nameParts[0] || '';
    const lastName = nameParts.slice(1).join(' ') || '';
    
    const response = await salesApiClient.createCustomer({
      firstName,
      lastName,
      email: customer.email || '',
      phone: customer.phone,
    });
    return mapCustomerResponse(response);
  }
}

export const salesService = new SalesService();
export default salesService;
