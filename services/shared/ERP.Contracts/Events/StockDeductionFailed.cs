namespace ERP.Contracts.Events;

public record StockDeductionFailed
{
    public Guid CorrelationId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
