using MassTransit;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;
using ERP.Contracts.Events.Domain;

namespace DashboardAnalytics.Consumers;

public class TransactionCreatedConsumer : IConsumer<TransactionCreated>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionCreatedConsumer> _logger;

    public TransactionCreatedConsumer(IServiceScopeFactory scopeFactory, ILogger<TransactionCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionCreated> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing TransactionCreated event for transaction {TransactionId}", msg.TransactionId);

        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.ProcessTransactionEventAsync(new TransactionEventDTO(
            msg.TransactionId, msg.TransactionId, msg.Timestamp, msg.Type, msg.TotalAmount, "TransactionCreated", msg.Timestamp));
    }
}

public class BudgetExceededConsumer : IConsumer<BudgetExceeded>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BudgetExceededConsumer> _logger;

    public BudgetExceededConsumer(IServiceScopeFactory scopeFactory, ILogger<BudgetExceededConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BudgetExceeded> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing BudgetExceeded event for budget {BudgetId}", msg.BudgetId);

        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        var exceeded = msg.CurrentSpending - msg.BudgetAmount;
        var pct = msg.BudgetAmount > 0 ? (msg.CurrentSpending / msg.BudgetAmount) * 100m : 0m;
        await analytics.ProcessBudgetExceededAlertAsync(new BudgetEventDTO(
            msg.BudgetId, msg.BudgetName, "", msg.BudgetAmount, msg.CurrentSpending, exceeded > 0 ? exceeded : 0, pct, msg.Timestamp));
    }
}
