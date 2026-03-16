namespace ERP.Contracts.Events;

public record ReturnCompleted
{
    public Guid CorrelationId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int QuantityReturned { get; init; }
    public int NewStock { get; init; }
    public decimal RefundAmount { get; init; }
}
