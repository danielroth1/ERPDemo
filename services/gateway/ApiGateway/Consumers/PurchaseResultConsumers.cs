using MassTransit;
using ERP.Contracts.Events;
using ApiGateway.Services;

namespace ApiGateway.Consumers;

public class PurchaseCompletedConsumer : IConsumer<PurchaseCompleted>
{
    private readonly PurchaseTracker _tracker;
    private readonly ILogger<PurchaseCompletedConsumer> _logger;

    public PurchaseCompletedConsumer(PurchaseTracker tracker, ILogger<PurchaseCompletedConsumer> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<PurchaseCompleted> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Purchase completed for {CorrelationId}: {ProductName} x{Qty}",
            msg.CorrelationId, msg.ProductName, msg.QuantityPurchased);

        _tracker.TryComplete(msg.CorrelationId, msg);
        return Task.CompletedTask;
    }
}

public class PurchaseFailedConsumer : IConsumer<PurchaseFailed>
{
    private readonly PurchaseTracker _tracker;
    private readonly ILogger<PurchaseFailedConsumer> _logger;

    public PurchaseFailedConsumer(PurchaseTracker tracker, ILogger<PurchaseFailedConsumer> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<PurchaseFailed> context)
    {
        var msg = context.Message;
        _logger.LogWarning("Purchase failed for {CorrelationId}: {Reason}",
            msg.CorrelationId, msg.Reason);

        _tracker.TryFail(msg.CorrelationId, msg.Reason);
        return Task.CompletedTask;
    }
}
