namespace ERP.Contracts.Events.Domain;

public record ProductCreated
{
    public string EventType => "ProductCreated";
    public string ProductId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? CategoryId { get; init; }
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record ProductUpdated
{
    public string EventType => "ProductUpdated";
    public string ProductId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? CategoryId { get; init; }
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record ProductDeleted
{
    public string EventType => "ProductDeleted";
    public string ProductId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record StockUpdated
{
    public string EventType => "StockUpdated";
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int OldQuantity { get; init; }
    public int NewQuantity { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record LowStockAlert
{
    public string EventType => "LowStockAlert";
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int ReorderLevel { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record StockMovementCreated
{
    public string EventType => "StockMovementCreated";
    public string MovementId { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
