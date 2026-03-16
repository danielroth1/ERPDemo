using MassTransit;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;
using ERP.Contracts.Events.Domain;

namespace DashboardAnalytics.Consumers;

public class UserCreatedConsumer : IConsumer<UserCreated>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserCreatedConsumer> _logger;

    public UserCreatedConsumer(IServiceScopeFactory scopeFactory, ILogger<UserCreatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserCreated> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing UserCreated event for user {UserId}", msg.UserId);

        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.ProcessUserEventAsync(new UserEventDTO(
            msg.UserId, msg.Email, msg.Role, "UserCreated", msg.Timestamp));
    }
}

public class UserUpdatedConsumer : IConsumer<UserUpdated>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserUpdatedConsumer> _logger;

    public UserUpdatedConsumer(IServiceScopeFactory scopeFactory, ILogger<UserUpdatedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserUpdated> context)
    {
        var msg = context.Message;
        using var scope = _scopeFactory.CreateScope();
        var analytics = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
        await analytics.ProcessUserEventAsync(new UserEventDTO(
            msg.UserId, msg.Email, msg.Role, "UserUpdated", msg.Timestamp));
    }
}
