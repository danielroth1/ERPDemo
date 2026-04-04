using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Models;

[Table("products")]
[Index(nameof(Sku), IsUnique = true)]
[Index(nameof(CategoryId))]
public class Product
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;

    [Precision(18, 2)]
    public decimal Price { get; set; }

    [Precision(18, 2)]
    public decimal Cost { get; set; }

    public int StockQuantity { get; set; }

    public int ReservedQuantity { get; set; }

    public int MinStockLevel { get; set; } = 10;

    public int MaxStockLevel { get; set; } = 1000;

    [MaxLength(20)]
    public string Unit { get; set; } = "pcs";

    public bool IsActive { get; set; } = true;

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    [NotMapped]
    public bool IsLowStock => AvailableQuantity <= MinStockLevel;
}
