using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement.Models;

[Table("orders")]
[Index(nameof(CustomerId))]
[Index(nameof(OrderNumber), IsUnique = true)]
[Index(nameof(Status))]
public class Order
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string CustomerId { get; set; } = string.Empty;

    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public List<OrderItem> Items { get; set; } = new();

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal Tax { get; set; }

    [Precision(18, 2)]
    public decimal Discount { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    public string? Notes { get; set; }

    [Column(TypeName = "jsonb")]
    public Address? ShippingAddress { get; set; }

    [Column(TypeName = "jsonb")]
    public Address? BillingAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }
}

[Table("customers")]
[Index(nameof(Email), IsUnique = true)]
public class Customer
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(255)]
    public string? Company { get; set; }

    [MaxLength(50)]
    public string? TaxId { get; set; }

    [Column(TypeName = "jsonb")]
    public Address? DefaultBillingAddress { get; set; }

    [Column(TypeName = "jsonb")]
    public Address? DefaultShippingAddress { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("invoices")]
[Index(nameof(InvoiceNumber), IsUnique = true)]
[Index(nameof(OrderId))]
[Index(nameof(CustomerId))]
[Index(nameof(Status))]
public class Invoice
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; }

    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal Tax { get; set; }

    [Precision(18, 2)]
    public decimal Discount { get; set; }

    [Precision(18, 2)]
    public decimal Total { get; set; }

    [Precision(18, 2)]
    public decimal AmountPaid { get; set; }

    [Precision(18, 2)]
    public decimal AmountDue { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    [Column(TypeName = "jsonb")]
    public List<OrderItem> Items { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public Address? BillingAddress { get; set; }

    public string? Notes { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Address
{
    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

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
