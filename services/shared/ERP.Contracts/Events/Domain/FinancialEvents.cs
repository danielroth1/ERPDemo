namespace ERP.Contracts.Events.Domain;

public record TransactionCreated
{
    public string EventType => "TransactionCreated";
    public string TransactionId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record BudgetExceeded
{
    public string EventType => "BudgetExceeded";
    public string BudgetId { get; init; } = string.Empty;
    public string BudgetName { get; init; } = string.Empty;
    public decimal BudgetAmount { get; init; }
    public decimal CurrentSpending { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
