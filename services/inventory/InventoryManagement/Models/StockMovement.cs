using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Models;

[Table("stock_movements")]
[Index(nameof(ProductId))]
public class StockMovement
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string ProductId { get; set; } = string.Empty;

    public MovementType MovementType { get; set; }

    public int Quantity { get; set; }

    [MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
