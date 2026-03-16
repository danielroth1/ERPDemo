namespace ERP.Contracts.Events;

public record RefundTransactionFailed
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
