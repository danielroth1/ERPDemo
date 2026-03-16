namespace ERP.Contracts.Commands;

public record CreateRefundTransaction
{
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal RefundAmount { get; init; }
    public string AuthToken { get; init; } = string.Empty;
}
