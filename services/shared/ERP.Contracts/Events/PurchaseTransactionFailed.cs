namespace ERP.Contracts.Events;

public record PurchaseTransactionFailed
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
