using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using Orchestration.Sagas;

namespace Orchestration.Tests.Sagas;

public class PurchaseStateMachineTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<PurchaseStateMachine, PurchaseState> _sagaHarness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddSagaStateMachine<PurchaseStateMachine, PurchaseState>()
                    .InMemoryRepository();
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();

        _sagaHarness = _harness.GetSagaStateMachineHarness<PurchaseStateMachine, PurchaseState>();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task SubmitPurchase_ShouldTransitionToReservingStock()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        // Act
        await _harness.Bus.Publish(new SubmitPurchase
        {
            CorrelationId = correlationId,
            UserId = "user-1",
            ProductId = "prod-1",
            Quantity = 5,
            AuthToken = "test-token"
        });

        // Assert
        (await _sagaHarness.Exists(correlationId, x => x.ReservingStock, timeout: TimeSpan.FromSeconds(5)))
            .Should().NotBeNull();

        var instance = _sagaHarness.Sagas.ContainsInState(correlationId, _sagaHarness.StateMachine, x => x.ReservingStock);
        instance.Should().NotBeNull();
    }

    [Fact]
    public async Task StockReserved_ShouldTransitionToCreatingTransaction()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        await _harness.Bus.Publish(new SubmitPurchase
        {
            CorrelationId = correlationId,
            UserId = "user-1",
            ProductId = "prod-1",
            Quantity = 5,
            AuthToken = "test-token"
        });
        await _sagaHarness.Exists(correlationId, x => x.ReservingStock, timeout: TimeSpan.FromSeconds(5));

        // Act
        await _harness.Bus.Publish(new StockReserved
        {
            CorrelationId = correlationId,
            ProductId = "prod-1",
            ProductName = "Widget",
            Quantity = 5,
            UnitPrice = 20.00m,
            RemainingStock = 45
        });

        // Assert
        (await _sagaHarness.Exists(correlationId, x => x.CreatingTransaction, timeout: TimeSpan.FromSeconds(5)))
            .Should().NotBeNull();
    }

    [Fact]
    public async Task StockReservationFailed_ShouldTransitionToFaulted()
    {
        // Arrange
        var correlationId = Guid.NewGuid();
        await _harness.Bus.Publish(new SubmitPurchase
        {
            CorrelationId = correlationId,
            UserId = "user-1",
            ProductId = "prod-1",
            Quantity = 5,
            AuthToken = "test-token"
        });
        await _sagaHarness.Exists(correlationId, x => x.ReservingStock, timeout: TimeSpan.FromSeconds(5));

        // Act
        await _harness.Bus.Publish(new StockReservationFailed
        {
            CorrelationId = correlationId,
            Reason = "Insufficient stock"
        });

        // Assert — saga is finalized (removed) after Faulted, verify via published event
        await Task.Delay(500); // Allow message processing
        (await _harness.Published.Any<PurchaseFailed>(
            x => x.Context.Message.CorrelationId == correlationId))
            .Should().BeTrue();
    }

    [Fact]
    public async Task PurchaseTransactionCreated_ShouldTransitionToDeductingStock()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        // Walk through state transitions
        await _harness.Bus.Publish(new SubmitPurchase
        {
            CorrelationId = correlationId,
            UserId = "user-1",
            ProductId = "prod-1",
            Quantity = 5,
            AuthToken = "test-token"
        });
        await _sagaHarness.Exists(correlationId, x => x.ReservingStock, timeout: TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new StockReserved
        {
            CorrelationId = correlationId,
            ProductId = "prod-1",
            ProductName = "Widget",
            Quantity = 5,
            UnitPrice = 20.00m,
            RemainingStock = 45
        });
        await _sagaHarness.Exists(correlationId, x => x.CreatingTransaction, timeout: TimeSpan.FromSeconds(5));

        // Act
        await _harness.Bus.Publish(new PurchaseTransactionCreated
        {
            CorrelationId = correlationId,
            TransactionId = "txn-1"
        });

        // Assert
        (await _sagaHarness.Exists(correlationId, x => x.DeductingStock, timeout: TimeSpan.FromSeconds(5)))
            .Should().NotBeNull();
    }

    [Fact]
    public async Task StockDeducted_ShouldTransitionToCompleted()
    {
        // Arrange — walk through full happy path
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new SubmitPurchase
        {
            CorrelationId = correlationId,
            UserId = "user-1",
            ProductId = "prod-1",
            Quantity = 5,
            AuthToken = "test-token"
        });
        await _sagaHarness.Exists(correlationId, x => x.ReservingStock, timeout: TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new StockReserved
        {
            CorrelationId = correlationId,
            ProductId = "prod-1",
            ProductName = "Widget",
            Quantity = 5,
            UnitPrice = 20.00m,
            RemainingStock = 45
        });
        await _sagaHarness.Exists(correlationId, x => x.CreatingTransaction, timeout: TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new PurchaseTransactionCreated
        {
            CorrelationId = correlationId,
            TransactionId = "txn-1"
        });
        await _sagaHarness.Exists(correlationId, x => x.DeductingStock, timeout: TimeSpan.FromSeconds(5));

        // Act
        await _harness.Bus.Publish(new StockDeducted
        {
            CorrelationId = correlationId,
            RemainingStock = 40
        });

        // Assert — saga is finalized (removed) after Completed, verify via published event
        await Task.Delay(500); // Allow message processing
        (await _harness.Published.Any<PurchaseCompleted>(
            x => x.Context.Message.CorrelationId == correlationId))
            .Should().BeTrue();
    }

    [Fact]
    public async Task PurchaseTransactionFailed_ShouldCompensateAndTransitionToFaulted()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        await _harness.Bus.Publish(new SubmitPurchase
        {
            CorrelationId = correlationId,
            UserId = "user-1",
            ProductId = "prod-1",
            Quantity = 5,
            AuthToken = "test-token"
        });
        await _sagaHarness.Exists(correlationId, x => x.ReservingStock, timeout: TimeSpan.FromSeconds(5));

        await _harness.Bus.Publish(new StockReserved
        {
            CorrelationId = correlationId,
            ProductId = "prod-1",
            ProductName = "Widget",
            Quantity = 5,
            UnitPrice = 20.00m,
            RemainingStock = 45
        });
        await _sagaHarness.Exists(correlationId, x => x.CreatingTransaction, timeout: TimeSpan.FromSeconds(5));

        // Act
        await _harness.Bus.Publish(new PurchaseTransactionFailed
        {
            CorrelationId = correlationId,
            Reason = "Account not found"
        });

        // Assert — saga is finalized (removed) after Faulted, verify via published event
        await Task.Delay(500); // Allow message processing
        (await _harness.Published.Any<PurchaseFailed>(
            x => x.Context.Message.CorrelationId == correlationId))
            .Should().BeTrue();
    }
}
