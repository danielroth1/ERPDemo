using MassTransit;
using FinancialManagement.Services;
using ERP.Contracts.Commands;

namespace FinancialManagement.Consumers;

public class VoidPurchaseTransactionConsumer : IConsumer<VoidPurchaseTransaction>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VoidPurchaseTransactionConsumer> _logger;

    public VoidPurchaseTransactionConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<VoidPurchaseTransactionConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VoidPurchaseTransaction> context)
    {
        var command = context.Message;
        _logger.LogInformation(
            "Voiding purchase transaction {TransactionId} for correlation {CorrelationId}. Reason: {Reason}",
            command.TransactionId, command.CorrelationId, command.Reason);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var transactionService = scope.ServiceProvider.GetRequiredService<ITransactionService>();

            var result = await transactionService.VoidTransactionAsync(command.TransactionId);
            if (result != null)
            {
                _logger.LogInformation(
                    "Transaction {TransactionId} voided successfully for correlation {CorrelationId}",
                    command.TransactionId, command.CorrelationId);
            }
            else
            {
                _logger.LogWarning(
                    "Transaction {TransactionId} not found or already voided for correlation {CorrelationId}",
                    command.TransactionId, command.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to void transaction {TransactionId} for correlation {CorrelationId}",
                command.TransactionId, command.CorrelationId);
        }
    }
}
