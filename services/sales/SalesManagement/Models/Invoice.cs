using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SalesManagement.Models;

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
