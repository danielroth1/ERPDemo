namespace ERP.Contracts.Events;

public record PurchaseCompleted
{
    public Guid CorrelationId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int QuantityPurchased { get; init; }
    public int RemainingStock { get; init; }
    public decimal TotalCost { get; init; }
}
