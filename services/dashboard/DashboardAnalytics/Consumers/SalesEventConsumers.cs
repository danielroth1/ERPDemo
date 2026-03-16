using MassTransit;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;
using ERP.Contracts.Events.Domain;

namespace DashboardAnalytics.Consumers;

public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public OrderCreatedConsumer(IServiceScopeFactory scopeFactory, ILogger<OrderCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing OrderCreated event for order {OrderId}", msg.OrderId);

        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.ProcessOrderEventAsync(new OrderEventDTO(
            msg.OrderId, msg.CustomerId, msg.TotalAmount, msg.Status, 0, "OrderCreated", msg.Timestamp));
    }
}

public class OrderStatusChangedConsumer : IConsumer<OrderStatusChanged>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderStatusChangedConsumer> _logger;

    public OrderStatusChangedConsumer(IServiceScopeFactory scopeFactory, ILogger<OrderStatusChangedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderStatusChanged> context)
    {
        var msg = context.Message;
        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.ProcessOrderEventAsync(new OrderEventDTO(
            msg.OrderId, "", 0m, msg.NewStatus, 0, "OrderStatusChanged", msg.Timestamp));
    }
}
