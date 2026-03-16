namespace ERP.Contracts.Events;

public record RefundTransactionCreated
{
    public Guid CorrelationId { get; init; }
    public string TransactionId { get; init; } = string.Empty;
}
