using MassTransit;
using FinancialManagement.Services;
using ERP.Contracts.Events.Domain;

namespace FinancialManagement.Consumers;

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
        var evt = context.Message;
        _logger.LogInformation("Creating financial accounts for new user {UserId} ({Email})", evt.UserId, evt.Email);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

            var userName = $"{evt.FirstName} {evt.LastName}".Trim();
            if (string.IsNullOrEmpty(userName)) userName = evt.Email;

            await accountService.CreateUserAccountsAsync(evt.UserId, userName);

            _logger.LogInformation("Financial accounts created for user {UserId}", evt.UserId);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("already has accounts"))
        {
            _logger.LogInformation("Financial accounts already exist for user {UserId}, skipping", evt.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create financial accounts for user {UserId}", evt.UserId);
            throw;
        }
    }
}
