using MassTransit;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Services;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;

namespace FinancialManagement.Consumers;

public class CreateRefundTransactionConsumer : IConsumer<CreateRefundTransaction>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CreateRefundTransactionConsumer> _logger;

    public CreateRefundTransactionConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<CreateRefundTransactionConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreateRefundTransaction> context)
    {
        var command = context.Message;
        _logger.LogInformation("Creating refund transaction for product {ProductId}, user {UserId}, correlation {CorrelationId}",
            command.ProductId, command.UserId, command.CorrelationId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

            // Resolve user account
            var userAccount = await accountService.GetAccountByUserIdAsync(command.UserId);

            // Resolve revenue account
            var systemAccounts = await accountService.GetSystemAccountsAsync();
            var revenueAccount = systemAccounts.FirstOrDefault(a => a.Name == "Product Sales Revenue");

            if (userAccount == null || revenueAccount == null)
            {
                _logger.LogError("Failed to resolve accounts for refund transaction. User: {UserId}", command.UserId);
                await context.Publish(new RefundTransactionFailed
                {
                    CorrelationId = command.CorrelationId,
                    Reason = "Failed to resolve financial accounts"
                });
                return;
            }

            var request = new CreateTransactionRequest
            {
                Description = $"Refund for return of {command.ProductName} (Qty: {command.Quantity})",
                Type = "Return",
                ReferenceId = command.ProductId,
                ReferenceType = "Product",
                Entries = new List<JournalEntryRequest>
                {
                    new() { AccountId = userAccount.Id, Debit = command.RefundAmount, Credit = 0m, Memo = $"Refund for {command.ProductName}" },
                    new() { AccountId = revenueAccount.Id, Debit = 0m, Credit = command.RefundAmount, Memo = $"Revenue reversal for {command.ProductName} return" }
                }
            };

            var result = await transactionService.CreateTransactionAsync(request, command.UserId);

            _logger.LogInformation("Refund transaction created: {TransactionId} for correlation {CorrelationId}",
                result.Id, command.CorrelationId);

            await context.Publish(new RefundTransactionCreated
            {
                CorrelationId = command.CorrelationId,
                TransactionId = result.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create refund transaction for correlation {CorrelationId}", command.CorrelationId);
            await context.Publish(new RefundTransactionFailed
            {
                CorrelationId = command.CorrelationId,
                Reason = ex.Message
            });
        }
    }
}
