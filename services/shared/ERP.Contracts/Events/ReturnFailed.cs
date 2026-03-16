namespace ERP.Contracts.Events;

public record ReturnFailed
{
    public Guid CorrelationId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
