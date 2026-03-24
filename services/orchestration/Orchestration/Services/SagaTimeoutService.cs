using Microsoft.EntityFrameworkCore;
using MassTransit;
using Orchestration.Infrastructure;
using Orchestration.Sagas;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;

namespace Orchestration.Services;

public class SagaTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaTimeoutService> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SagaTimeout = TimeSpan.FromMinutes(5);

    public SagaTimeoutService(
        IServiceProvider serviceProvider,
        ILogger<SagaTimeoutService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SagaTimeoutService started. Checking every {Interval} for sagas stuck longer than {Timeout}",
            CheckInterval, SagaTimeout);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);

            try
            {
                await CheckStuckPurchaseSagasAsync(stoppingToken);
                await CheckStuckReturnSagasAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error checking for stuck sagas");
            }
        }
    }

    private async Task CheckStuckPurchaseSagasAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrchestrationDbContext>();
        var sendEndpoint = scope.ServiceProvider.GetRequiredService<ISendEndpointProvider>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var cutoff = DateTime.UtcNow - SagaTimeout;
        var stuckSagas = await dbContext.Set<PurchaseState>()
            .Where(s => s.UpdatedAt < cutoff)
            .Where(s => s.CurrentState != "Completed" && s.CurrentState != "Faulted" && s.CurrentState != "Final")
            .ToListAsync(ct);

        if (stuckSagas.Count == 0) return;

        _logger.LogWarning("Found {Count} stuck purchase sagas", stuckSagas.Count);

        foreach (var saga in stuckSagas)
        {
            _logger.LogWarning(
                "Purchase saga {CorrelationId} stuck in state {State} since {UpdatedAt}. Triggering compensation.",
                saga.CorrelationId, saga.CurrentState, saga.UpdatedAt);

            switch (saga.CurrentState)
            {
                case "DeductingStock":
                    if (!string.IsNullOrEmpty(saga.TransactionId))
                    {
                        var ep = await sendEndpoint.GetSendEndpoint(new Uri("queue:void-purchase-transaction"));
                        await ep.Send(new VoidPurchaseTransaction
                        {
                            CorrelationId = saga.CorrelationId,
                            TransactionId = saga.TransactionId,
                            Reason = "Saga timed out waiting for stock deduction"
                        }, ct);
                    }
                    break;

                case "CreatingTransaction":
                    // Stock was reserved but transaction never created — release reservation
                    {
                        var ep = await sendEndpoint.GetSendEndpoint(new Uri("queue:release-reservation"));
                        await ep.Send(new ReleaseReservation
                        {
                            CorrelationId = saga.CorrelationId,
                            ProductId = saga.ProductId,
                            Quantity = saga.Quantity
                        }, ct);
                    }
                    break;

                case "ReservingStock":
                    // Nothing committed yet — no compensation needed
                    break;
            }

            saga.CurrentState = "Faulted";
            saga.FailureReason = $"Saga timed out in state {saga.CurrentState} after {SagaTimeout.TotalMinutes} minutes";
            saga.UpdatedAt = DateTime.UtcNow;

            await publishEndpoint.Publish(new PurchaseFailed
            {
                CorrelationId = saga.CorrelationId,
                Reason = saga.FailureReason
            }, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task CheckStuckReturnSagasAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrchestrationDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var cutoff = DateTime.UtcNow - SagaTimeout;
        var stuckSagas = await dbContext.Set<ReturnState>()
            .Where(s => s.UpdatedAt < cutoff)
            .Where(s => s.CurrentState != "Completed" && s.CurrentState != "Faulted" && s.CurrentState != "Final")
            .ToListAsync(ct);

        if (stuckSagas.Count == 0) return;

        _logger.LogWarning("Found {Count} stuck return sagas", stuckSagas.Count);

        foreach (var saga in stuckSagas)
        {
            _logger.LogWarning(
                "Return saga {CorrelationId} stuck in state {State} since {UpdatedAt}. Triggering compensation.",
                saga.CorrelationId, saga.CurrentState, saga.UpdatedAt);

            saga.CurrentState = "Faulted";
            saga.FailureReason = $"Saga timed out in state {saga.CurrentState} after {SagaTimeout.TotalMinutes} minutes";
            saga.UpdatedAt = DateTime.UtcNow;

            await publishEndpoint.Publish(new ReturnFailed
            {
                CorrelationId = saga.CorrelationId,
                Reason = saga.FailureReason
            }, ct);
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
