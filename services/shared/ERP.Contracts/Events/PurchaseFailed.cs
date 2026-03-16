namespace ERP.Contracts.Events;

public record PurchaseFailed
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
