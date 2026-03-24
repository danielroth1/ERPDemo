using MassTransit;

namespace Orchestration.Sagas;

public class PurchaseState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    // Original request data
    public string UserId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string AuthToken { get; set; } = string.Empty;

    // Data collected during saga
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalRevenue { get; set; }
    public int RemainingStock { get; set; }
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }

    // Timestamps for timeout detection
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
