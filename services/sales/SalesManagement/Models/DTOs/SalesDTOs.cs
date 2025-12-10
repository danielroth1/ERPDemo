namespace SalesManagement.Models.DTOs;

// Order DTOs
public class CreateOrderRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItemRequest> Items { get; set; } = new();
    public decimal Discount { get; set; }
    public string? Notes { get; set; }
    public AddressRequest? ShippingAddress { get; set; }
    public AddressRequest? BillingAddress { get; set; }
}

public class UpdateOrderRequest
{
    public List<OrderItemRequest>? Items { get; set; }
    public decimal? Discount { get; set; }
    public string? Notes { get; set; }
    public AddressRequest? ShippingAddress { get; set; }
    public AddressRequest? BillingAddress { get; set; }
}

public class OrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Discount { get; set; }
}

public class OrderResponse
{
    public string Id { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Address? ShippingAddress { get; set; }
    public Address? BillingAddress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
}

// Customer DTOs
public class CreateCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? TaxId { get; set; }
    public AddressRequest? DefaultBillingAddress { get; set; }
    public AddressRequest? DefaultShippingAddress { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCustomerRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? TaxId { get; set; }
    public AddressRequest? DefaultBillingAddress { get; set; }
    public AddressRequest? DefaultShippingAddress { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public class CustomerResponse
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string? TaxId { get; set; }
    public Address? DefaultBillingAddress { get; set; }
    public Address? DefaultShippingAddress { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Invoice DTOs
public class CreateInvoiceRequest
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class UpdateInvoiceRequest
{
    public DateTime? DueDate { get; set; }
    public string? Notes { get; set; }
}

public class InvoiceResponse
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public Address? BillingAddress { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RecordPaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}

// Common DTOs
public class AddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

// Kafka Events
public class OrderCreatedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<OrderItemEvent> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class OrderStatusChangedEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

public class OrderItemEvent
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class InvoiceCreatedEvent
{
    public string InvoiceId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InvoicePaidEvent
{
    public string InvoiceId { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public DateTime PaidAt { get; set; }
}
