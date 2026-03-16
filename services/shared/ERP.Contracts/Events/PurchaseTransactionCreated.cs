namespace ERP.Contracts.Events;

public record PurchaseTransactionCreated
{
    public Guid CorrelationId { get; init; }
    public string TransactionId { get; init; } = string.Empty;
}
