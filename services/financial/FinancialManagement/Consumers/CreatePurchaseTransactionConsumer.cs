using MassTransit;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Services;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;

namespace FinancialManagement.Consumers;

public class CreatePurchaseTransactionConsumer : IConsumer<CreatePurchaseTransaction>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITopicProducer<PurchaseTransactionCreated> _purchaseTransactionCreatedProducer;
    private readonly ITopicProducer<PurchaseTransactionFailed> _purchaseTransactionFailedProducer;
    private readonly ILogger<CreatePurchaseTransactionConsumer> _logger;

    public CreatePurchaseTransactionConsumer(
        IServiceScopeFactory scopeFactory,
        ITopicProducer<PurchaseTransactionCreated> purchaseTransactionCreatedProducer,
        ITopicProducer<PurchaseTransactionFailed> purchaseTransactionFailedProducer,
        ILogger<CreatePurchaseTransactionConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _purchaseTransactionCreatedProducer = purchaseTransactionCreatedProducer;
        _purchaseTransactionFailedProducer = purchaseTransactionFailedProducer;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CreatePurchaseTransaction> context)
    {
        var command = context.Message;
        _logger.LogInformation("Creating purchase transaction for product {ProductId}, user {UserId}, correlation {CorrelationId}",
            command.ProductId, command.UserId, command.CorrelationId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();
            var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();

            // Resolve user accounts
            var userAccount = await accountService.GetAccountByUserIdAsync(command.UserId);
            var userExpenseAccount = await accountService.GetAccountByUserIdAndTypeAsync(command.UserId, AccountType.Expense);

            // Resolve system accounts
            var systemAccounts = await accountService.GetSystemAccountsAsync();
            var companyAccount = systemAccounts.FirstOrDefault(a => a.Name == "Company Operating Account");
            var taxAccount = systemAccounts.FirstOrDefault(a => a.Name == "Sales Tax Payable");
            var revenueAccount = systemAccounts.FirstOrDefault(a => a.Name == "Product Sales Revenue");
            var inventoryAccount = systemAccounts.FirstOrDefault(a => a.Name == "Product Inventory");
            var cogsAccount = systemAccounts.FirstOrDefault(a => a.Name == "Cost of Goods Sold");

            if (userAccount == null || userExpenseAccount == null || companyAccount == null ||
                taxAccount == null || revenueAccount == null || inventoryAccount == null || cogsAccount == null)
            {
                _logger.LogError("Failed to resolve one or more accounts for purchase transaction. User: {UserId}", command.UserId);
                await _purchaseTransactionFailedProducer.Produce(new PurchaseTransactionFailed
                {
                    CorrelationId = command.CorrelationId,
                    Reason = "Failed to resolve financial accounts"
                });
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
                    new() { AccountId = userAccount.Id, Debit = 0m, Credit = command.TotalCost, Memo = $"Payment for {command.ProductName}" },
                    new() { AccountId = userExpenseAccount.Id, Debit = command.TotalCost, Credit = 0m, Memo = $"Expense for {command.ProductName}" },
                    new() { AccountId = companyAccount.Id, Debit = command.TotalCost, Credit = 0m, Memo = $"Payment received for {command.ProductName}" },
                    new() { AccountId = taxAccount.Id, Debit = 0m, Credit = command.TotalTax, Memo = $"Sales tax collected for {command.ProductName}" },
                    new() { AccountId = revenueAccount.Id, Debit = 0m, Credit = command.TotalRevenue, Memo = $"Revenue from sale of {command.ProductName}" },
                    new() { AccountId = inventoryAccount.Id, Debit = 0m, Credit = command.TotalCost, Memo = $"Product leaving inventory: {command.ProductName}" },
                    new() { AccountId = cogsAccount.Id, Debit = command.TotalCost, Credit = 0m, Memo = $"Cost of goods sold: {command.ProductName}" },
                }
            };

            var result = await transactionService.CreateTransactionAsync(request, command.UserId);

            _logger.LogInformation("Purchase transaction created: {TransactionId} for correlation {CorrelationId}",
                result.Id, command.CorrelationId);

            await _purchaseTransactionCreatedProducer.Produce(new PurchaseTransactionCreated
            {
                CorrelationId = command.CorrelationId,
                TransactionId = result.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create purchase transaction for correlation {CorrelationId}", command.CorrelationId);
            await _purchaseTransactionFailedProducer.Produce(new PurchaseTransactionFailed
            {
                CorrelationId = command.CorrelationId,
                Reason = ex.Message
            });
        }
    }
}
