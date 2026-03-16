namespace ERP.Contracts.Events;

public record StockReserved
{
    public Guid CorrelationId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public int RemainingStock { get; init; }
}
