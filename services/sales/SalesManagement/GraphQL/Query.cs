using SalesManagement.Models.DTOs;
using SalesManagement.Services;

namespace SalesManagement.GraphQL;

public class Query
{
    /// <summary>
    /// Get order by ID
    /// </summary>
    public async Task<OrderResponse?> GetOrder(
        [Service] IOrderService orderService,
        string id)
    {
        return await orderService.GetOrderByIdAsync(id);
    }

    /// <summary>
    /// Get all orders
    /// </summary>
    public async Task<List<OrderResponse>> GetOrders(
        [Service] IOrderService orderService,
        int skip = 0,
        int limit = 100)
    {
        return await orderService.GetAllOrdersAsync(skip, limit);
    }

    /// <summary>
    /// Get orders by customer
    /// </summary>
    public async Task<List<OrderResponse>> GetOrdersByCustomer(
        [Service] IOrderService orderService,
        string customerId,
        int skip = 0,
        int limit = 100)
    {
        return await orderService.GetOrdersByCustomerAsync(customerId, skip, limit);
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    public async Task<CustomerResponse?> GetCustomer(
        [Service] ICustomerService customerService,
        string id)
    {
        return await customerService.GetCustomerByIdAsync(id);
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    public async Task<List<CustomerResponse>> GetCustomers(
        [Service] ICustomerService customerService,
        int skip = 0,
        int limit = 100)
    {
        return await customerService.GetAllCustomersAsync(skip, limit);
    }

    /// <summary>
    /// Search customers
    /// </summary>
    public async Task<List<CustomerResponse>> SearchCustomers(
        [Service] ICustomerService customerService,
        string searchTerm,
        int skip = 0,
        int limit = 100)
    {
        return await customerService.SearchCustomersAsync(searchTerm, skip, limit);
    }

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    public async Task<InvoiceResponse?> GetInvoice(
        [Service] IInvoiceService invoiceService,
        string id)
    {
        return await invoiceService.GetInvoiceByIdAsync(id);
    }

    /// <summary>
    /// Get all invoices
    /// </summary>
    public async Task<List<InvoiceResponse>> GetInvoices(
        [Service] IInvoiceService invoiceService,
        int skip = 0,
        int limit = 100)
    {
        return await invoiceService.GetAllInvoicesAsync(skip, limit);
    }

    /// <summary>
    /// Get overdue invoices
    /// </summary>
    public async Task<List<InvoiceResponse>> GetOverdueInvoices(
        [Service] IInvoiceService invoiceService,
        int skip = 0,
        int limit = 100)
    {
        return await invoiceService.GetOverdueInvoicesAsync(skip, limit);
    }
}
