using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Infrastructure;
using FinancialManagement.Services;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using ERP.Contracts.Infrastructure;

namespace FinancialManagement.Consumers;

public class CreatePurchaseTransactionConsumer : IConsumer<CreatePurchaseTransaction>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CreatePurchaseTransactionConsumer> _logger;

    public CreatePurchaseTransactionConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<CreatePurchaseTransactionConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreatePurchaseTransaction> context)
    {
        var command = context.Message;
        var timer = new OperationTimer(_logger, "CreatePurchaseTransaction");
        _logger.LogInformation("Creating purchase transaction for product {ProductId}, user {UserId}, correlation {CorrelationId}",
            command.ProductId, command.UserId, command.CorrelationId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

            // Idempotency: check if already processed
            ProcessedMessage? existing;
            using (timer.Step("IdempotencyCheck"))
            {
                existing = await dbContext.ProcessedMessages
                    .FirstOrDefaultAsync(m => m.CorrelationId == command.CorrelationId && m.ConsumerName == nameof(CreatePurchaseTransactionConsumer));
            }

            if (existing != null)
            {
                _logger.LogWarning("Duplicate CreatePurchaseTransaction for correlation {CorrelationId}, re-producing result", command.CorrelationId);
                if (existing.Success && existing.ResponseData != null)
                    await context.Publish(JsonSerializer.Deserialize<PurchaseTransactionCreated>(existing.ResponseData)!);
                else if (!existing.Success && existing.ResponseData != null)
                    await context.Publish(JsonSerializer.Deserialize<PurchaseTransactionFailed>(existing.ResponseData)!);
                timer.LogSummary();
                return;
            }

            // Resolve all accounts in parallel
            Account? companyAccount, taxAccount, revenueAccount, inventoryAccount, cogsAccount;
            AccountResponse? userAccountResponse, userExpenseAccountResponse;
            using (timer.Step("ResolveAccounts"))
            {
                // Sequential — accountService shares the same AppDbContext scoped instance,
                // which is not thread-safe. Task.WhenAll would cause concurrent EF Core operations.
                userAccountResponse = await accountService.GetAccountByUserIdAsync(command.UserId);
                userExpenseAccountResponse = await accountService.GetAccountByUserIdAndTypeAsync(command.UserId, AccountType.Expense);
                var systemAccounts = await accountService.GetSystemAccountsAsync();
                companyAccount = systemAccounts.FirstOrDefault(a => a.Name == "Company Operating Account");
                taxAccount = systemAccounts.FirstOrDefault(a => a.Name == "Sales Tax Payable");
                revenueAccount = systemAccounts.FirstOrDefault(a => a.Name == "Product Sales Revenue");
                inventoryAccount = systemAccounts.FirstOrDefault(a => a.Name == "Product Inventory");
                cogsAccount = systemAccounts.FirstOrDefault(a => a.Name == "Cost of Goods Sold");
            }

            if (userAccountResponse == null || userExpenseAccountResponse == null || companyAccount == null ||
                taxAccount == null || revenueAccount == null || inventoryAccount == null || cogsAccount == null)
            {
                _logger.LogError("Failed to resolve one or more accounts for purchase transaction. User: {UserId}", command.UserId);
                var failEvent = new PurchaseTransactionFailed
                {
                    CorrelationId = command.CorrelationId,
                    Reason = "Failed to resolve financial accounts"
                };
                dbContext.ProcessedMessages.Add(new ProcessedMessage
                {
                    CorrelationId = command.CorrelationId,
                    ConsumerName = nameof(CreatePurchaseTransactionConsumer),
                    Success = false,
                    ResponseData = JsonSerializer.Serialize(failEvent)
                });
                await dbContext.SaveChangesAsync();
                await context.Publish(failEvent);
                timer.LogSummary();
                return;
            }

            var request = new CreateTransactionRequest
            {
                Description = $"Purchase of {command.ProductName} (Qty: {command.Quantity})",
                Type = "Sale",
                ReferenceId = command.ProductId,
                ReferenceType = "Product",
                Entries = new List<JournalEntryRequest>
                {
                    new() { AccountId = userAccountResponse.Id, Debit = 0m, Credit = command.TotalCost, Memo = $"Payment for {command.ProductName}" },
                    new() { AccountId = userExpenseAccountResponse.Id, Debit = command.TotalCost, Credit = 0m, Memo = $"Expense for {command.ProductName}" },
                    new() { AccountId = companyAccount.Id, Debit = command.TotalCost, Credit = 0m, Memo = $"Payment received for {command.ProductName}" },
                    new() { AccountId = taxAccount.Id, Debit = 0m, Credit = command.TotalTax, Memo = $"Sales tax collected for {command.ProductName}" },
                    new() { AccountId = revenueAccount.Id, Debit = 0m, Credit = command.TotalRevenue, Memo = $"Revenue from sale of {command.ProductName}" },
                    new() { AccountId = inventoryAccount.Id, Debit = 0m, Credit = command.TotalCost, Memo = $"Product leaving inventory: {command.ProductName}" },
                    new() { AccountId = cogsAccount.Id, Debit = command.TotalCost, Credit = 0m, Memo = $"Cost of goods sold: {command.ProductName}" },
                }
            };

            TransactionResponse result;
            using (timer.Step("CreateTransaction"))
            {
                result = await transactionService.CreateTransactionAsync(request, command.UserId);
            }

            _logger.LogInformation("Purchase transaction created: {TransactionId} for correlation {CorrelationId}",
                result.Id, command.CorrelationId);

            var successEvent = new PurchaseTransactionCreated
            {
                CorrelationId = command.CorrelationId,
                TransactionId = result.Id
            };

            // Record as processed (transaction was already saved by transactionService)
            dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                CorrelationId = command.CorrelationId,
                ConsumerName = nameof(CreatePurchaseTransactionConsumer),
                Success = true,
                ResponseData = JsonSerializer.Serialize(successEvent)
            });
            using (timer.Step("SaveProcessedMessage"))
            {
                await dbContext.SaveChangesAsync();
            }

            using (timer.Step("PublishTransactionCreated"))
            {
                await context.Publish(successEvent);
            }

            timer.LogSummary();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create purchase transaction for correlation {CorrelationId}", command.CorrelationId);
            await context.Publish(new PurchaseTransactionFailed
            {
                CorrelationId = command.CorrelationId,
                Reason = ex.Message
            });
        }
    }
}
