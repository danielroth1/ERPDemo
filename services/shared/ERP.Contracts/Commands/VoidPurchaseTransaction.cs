namespace ERP.Contracts.Commands;

public record VoidPurchaseTransaction
{
    public Guid CorrelationId { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
