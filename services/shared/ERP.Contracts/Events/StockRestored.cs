namespace ERP.Contracts.Events;

public record StockRestored
{
    public Guid CorrelationId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int NewStock { get; init; }
}
