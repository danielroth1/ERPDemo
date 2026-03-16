namespace ERP.Contracts.Events.Domain;

public record OrderCreated
{
    public string EventType => "OrderCreated";
    public string OrderId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record OrderStatusChanged
{
    public string EventType => "OrderStatusChanged";
    public string OrderId { get; init; } = string.Empty;
    public string OldStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record InvoiceCreated
{
    public string EventType => "InvoiceCreated";
    public string InvoiceId { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record InvoicePaid
{
    public string EventType => "InvoicePaid";
    public string InvoiceId { get; init; } = string.Empty;
    public string OrderId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
