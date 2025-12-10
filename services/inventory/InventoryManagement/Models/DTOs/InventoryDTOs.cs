namespace InventoryManagement.Models.DTOs;

public class ProductRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; } = 10;
    public int MaxStockLevel { get; set; } = 1000;
    public string Unit { get; set; } = "pcs";
}

public class ProductResponse
{
    public string Id { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLowStock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CategoryResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StockMovementRequest
{
    public string ProductId { get; set; } = string.Empty;
    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class StockMovementResponse
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public MovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StockAdjustmentRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int NewQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class LowStockAlert
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStockLevel { get; set; }
    public int ReorderQuantity { get; set; }
}

public class PurchaseResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityPurchased { get; set; }
    public int RemainingStock { get; set; }
    public decimal TotalCost { get; set; }
}

public class ReturnResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityReturned { get; set; }
    public int NewStock { get; set; }
    public decimal RefundAmount { get; set; }
}

public class EmptyResponse
{
    // Empty response object to avoid anonymous types
}

public class StockAdjustmentResponse
{
    public int NewQuantity { get; set; }
}

public class SeedProductsResponse
{
    public int ProductsCreated { get; set; }
    public int ProductsDeleted { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class CreateProductRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
