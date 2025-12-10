using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SalesManagement.Models;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [BsonElement("items")]
    public List<OrderItem> Items { get; set; } = new();

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    [BsonElement("tax")]
    public decimal Tax { get; set; }

    [BsonElement("discount")]
    public decimal Discount { get; set; }

    [BsonElement("total")]
    public decimal Total { get; set; }

    [BsonElement("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("shippingAddress")]
    public Address? ShippingAddress { get; set; }

    [BsonElement("billingAddress")]
    public Address? BillingAddress { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("cancelledAt")]
    public DateTime? CancelledAt { get; set; }
}

public class OrderItem
{
    [BsonElement("productId")]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("unitPrice")]
    public decimal UnitPrice { get; set; }

    [BsonElement("discount")]
    public decimal Discount { get; set; }

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    [BsonElement("total")]
    public decimal Total { get; set; }
}

public class Customer
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("firstName")]
    public string FirstName { get; set; } = string.Empty;

    [BsonElement("lastName")]
    public string LastName { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("company")]
    public string? Company { get; set; }

    [BsonElement("taxId")]
    public string? TaxId { get; set; }

    [BsonElement("defaultBillingAddress")]
    public Address? DefaultBillingAddress { get; set; }

    [BsonElement("defaultShippingAddress")]
    public Address? DefaultShippingAddress { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Invoice
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [BsonElement("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("issueDate")]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    [BsonElement("dueDate")]
    public DateTime DueDate { get; set; }

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    [BsonElement("tax")]
    public decimal Tax { get; set; }

    [BsonElement("discount")]
    public decimal Discount { get; set; }

    [BsonElement("total")]
    public decimal Total { get; set; }

    [BsonElement("amountPaid")]
    public decimal AmountPaid { get; set; }

    [BsonElement("amountDue")]
    public decimal AmountDue { get; set; }

    [BsonElement("status")]
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [BsonElement("items")]
    public List<OrderItem> Items { get; set; } = new();

    [BsonElement("billingAddress")]
    public Address? BillingAddress { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Address
{
    [BsonElement("street")]
    public string Street { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    [BsonElement("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;
}

public enum OrderStatus
{
    Draft,
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Completed,
    Cancelled
}

public enum InvoiceStatus
{
    Draft,
    Pending,
    Sent,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}
