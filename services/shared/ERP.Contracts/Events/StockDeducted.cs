namespace ERP.Contracts.Events;

public record StockDeducted
{
    public Guid CorrelationId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int QuantityDeducted { get; init; }
    public int RemainingStock { get; init; }
    public decimal TotalCost { get; init; }
}
