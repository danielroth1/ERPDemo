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
