using SalesManagement.Models;
using SalesManagement.Models.DTOs;
using SalesManagement.Services;

namespace SalesManagement.GraphQL;

public class Mutation
{
    /// <summary>
    /// Create a new order
    /// </summary>
    public async Task<OrderResponse> CreateOrder(
        [Service] IOrderService orderService,
        CreateOrderRequest input)
    {
        return await orderService.CreateOrderAsync(input);
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    public async Task<OrderResponse?> UpdateOrder(
        [Service] IOrderService orderService,
        string id,
        UpdateOrderRequest input)
    {
        return await orderService.UpdateOrderAsync(id, input);
    }

    /// <summary>
    /// Update order status
    /// </summary>
    public async Task<OrderResponse?> UpdateOrderStatus(
        [Service] IOrderService orderService,
        string id,
        OrderStatus status)
    {
        return await orderService.UpdateOrderStatusAsync(id, status);
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    public async Task<bool> DeleteOrder(
        [Service] IOrderService orderService,
        string id)
    {
        return await orderService.DeleteOrderAsync(id);
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    public async Task<CustomerResponse> CreateCustomer(
        [Service] ICustomerService customerService,
        CreateCustomerRequest input)
    {
        return await customerService.CreateCustomerAsync(input);
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    public async Task<CustomerResponse?> UpdateCustomer(
        [Service] ICustomerService customerService,
        string id,
        UpdateCustomerRequest input)
    {
        return await customerService.UpdateCustomerAsync(id, input);
    }

    /// <summary>
    /// Delete a customer
    /// </summary>
    public async Task<bool> DeleteCustomer(
        [Service] ICustomerService customerService,
        string id)
    {
        return await customerService.DeleteCustomerAsync(id);
    }

    /// <summary>
    /// Create a new invoice
    /// </summary>
    public async Task<InvoiceResponse> CreateInvoice(
        [Service] IInvoiceService invoiceService,
        CreateInvoiceRequest input)
    {
        return await invoiceService.CreateInvoiceAsync(input);
    }

    /// <summary>
    /// Update an existing invoice
    /// </summary>
    public async Task<InvoiceResponse?> UpdateInvoice(
        [Service] IInvoiceService invoiceService,
        string id,
        UpdateInvoiceRequest input)
    {
        return await invoiceService.UpdateInvoiceAsync(id, input);
    }

    /// <summary>
    /// Record a payment for an invoice
    /// </summary>
    public async Task<InvoiceResponse?> RecordPayment(
        [Service] IInvoiceService invoiceService,
        string id,
        RecordPaymentRequest input)
    {
        return await invoiceService.RecordPaymentAsync(id, input);
    }

    /// <summary>
    /// Delete an invoice
    /// </summary>
    public async Task<bool> DeleteInvoice(
        [Service] IInvoiceService invoiceService,
        string id)
    {
        return await invoiceService.DeleteInvoiceAsync(id);
    }
}
