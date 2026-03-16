using MassTransit;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;
using ERP.Contracts.Events.Domain;

namespace DashboardAnalytics.Consumers;

public class ProductCreatedConsumer : IConsumer<ProductCreated>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(IServiceScopeFactory scopeFactory, ILogger<ProductCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreated> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing ProductCreated event for product {ProductId}", msg.ProductId);

        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.ProcessProductEventAsync(new ProductEventDTO(
            msg.ProductId, msg.Name, msg.CategoryId ?? "", msg.StockQuantity, msg.Price, "ProductCreated", msg.Timestamp));
    }
}

public class ProductUpdatedConsumer : IConsumer<ProductUpdated>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductUpdatedConsumer> _logger;

    public ProductUpdatedConsumer(IServiceScopeFactory scopeFactory, ILogger<ProductUpdatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductUpdated> context)
    {
        var msg = context.Message;
        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.ProcessProductEventAsync(new ProductEventDTO(
            msg.ProductId, msg.Name, msg.CategoryId ?? "", msg.StockQuantity, msg.Price, "ProductUpdated", msg.Timestamp));
    }
}

public class LowStockAlertConsumer : IConsumer<LowStockAlert>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LowStockAlertConsumer> _logger;

    public LowStockAlertConsumer(IServiceScopeFactory scopeFactory, ILogger<LowStockAlertConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LowStockAlert> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing LowStockAlert for product {ProductId}", msg.ProductId);

        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.ProcessLowStockAlertAsync(new ProductEventDTO(
            msg.ProductId, msg.ProductName, "", msg.CurrentStock, 0m, "LowStockAlert", msg.Timestamp));
    }
}
