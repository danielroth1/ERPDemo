using MassTransit;
using ERP.Contracts.Events;
using ApiGateway.Services;

namespace ApiGateway.Consumers;

public class ReturnCompletedConsumer : IConsumer<ReturnCompleted>
{
    private readonly ReturnTracker _tracker;
    private readonly ILogger<ReturnCompletedConsumer> _logger;

    public ReturnCompletedConsumer(ReturnTracker tracker, ILogger<ReturnCompletedConsumer> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ReturnCompleted> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Return completed for {CorrelationId}: {ProductName} x{Qty}",
            msg.CorrelationId, msg.ProductName, msg.QuantityReturned);

        _tracker.TryComplete(msg.CorrelationId, msg);
        return Task.CompletedTask;
    }
}

public class ReturnFailedConsumer : IConsumer<ReturnFailed>
{
    private readonly ReturnTracker _tracker;
    private readonly ILogger<ReturnFailedConsumer> _logger;

    public ReturnFailedConsumer(ReturnTracker tracker, ILogger<ReturnFailedConsumer> logger)
    {
        _tracker = tracker;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ReturnFailed> context)
    {
        var msg = context.Message;
        _logger.LogWarning("Return failed for {CorrelationId}: {Reason}",
            msg.CorrelationId, msg.Reason);

        _tracker.TryFail(msg.CorrelationId, msg.Reason);
        return Task.CompletedTask;
    }
}
