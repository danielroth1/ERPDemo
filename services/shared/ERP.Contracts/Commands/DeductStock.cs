namespace ERP.Contracts.Commands;

public record DeductStock
{
    public Guid CorrelationId { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
