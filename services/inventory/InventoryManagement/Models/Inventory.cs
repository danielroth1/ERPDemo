using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventoryManagement.Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("sku")]
    [BsonRequired]
    public string Sku { get; set; } = string.Empty;

    [BsonElement("name")]
    [BsonRequired]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("categoryId")]
    public string CategoryId { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("cost")]
    public decimal Cost { get; set; }

    [BsonElement("stockQuantity")]
    public int StockQuantity { get; set; }

    [BsonElement("minStockLevel")]
    public int MinStockLevel { get; set; } = 10;

    [BsonElement("maxStockLevel")]
    public int MaxStockLevel { get; set; } = 1000;

    [BsonElement("unit")]
    public string Unit { get; set; } = "pcs";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsLowStock => StockQuantity <= MinStockLevel;
}

public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    [BsonRequired]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class StockMovement
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("productId")]
    [BsonRequired]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("movementType")]
    public MovementType MovementType { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("reference")]
    public string Reference { get; set; } = string.Empty;

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;

    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum MovementType
{
    Purchase,
    Sale,
    Return,
    Adjustment,
    Transfer
}
