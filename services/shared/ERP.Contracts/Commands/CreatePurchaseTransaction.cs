namespace ERP.Contracts.Commands;

public record CreatePurchaseTransaction
{
    public Guid CorrelationId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal TotalCost { get; init; }
    public decimal TotalTax { get; init; }
    public decimal TotalRevenue { get; init; }
    public string AuthToken { get; init; } = string.Empty;
}
