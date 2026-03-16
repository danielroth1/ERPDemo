using MassTransit;

namespace ApiGateway.Sagas;

public class ReturnState : SagaStateMachineInstance
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
    public decimal RefundAmount { get; set; }
    public int NewStock { get; set; }
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
}
