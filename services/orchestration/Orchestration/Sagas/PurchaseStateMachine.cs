using MassTransit;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;

namespace Orchestration.Sagas;

public class PurchaseStateMachine : MassTransitStateMachine<PurchaseState>
{
    // States
    public State ReservingStock { get; private set; } = null!;
    public State CreatingTransaction { get; private set; } = null!;
    public State DeductingStock { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    // Events (all arrive via RabbitMQ)
    public Event<SubmitPurchase> SubmitPurchase { get; private set; } = null!;
    public Event<StockReserved> StockReserved { get; private set; } = null!;
    public Event<StockReservationFailed> StockReservationFailed { get; private set; } = null!;
    public Event<PurchaseTransactionCreated> PurchaseTransactionCreated { get; private set; } = null!;
    public Event<PurchaseTransactionFailed> PurchaseTransactionFailed { get; private set; } = null!;
    public Event<StockDeducted> StockDeducted { get; private set; } = null!;
    public Event<StockDeductionFailed> StockDeductionFailed { get; private set; } = null!;

    public PurchaseStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => SubmitPurchase, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockReservationFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PurchaseTransactionCreated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PurchaseTransactionFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockDeducted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockDeductionFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Initially(
            When(SubmitPurchase)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.ProductId = ctx.Message.ProductId;
                    ctx.Saga.Quantity = ctx.Message.Quantity;
                    ctx.Saga.AuthToken = ctx.Message.AuthToken;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Send(ctx => new Uri("queue:reserve-stock"), ctx => new ReserveStock
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    ProductId = ctx.Saga.ProductId,
                    Quantity = ctx.Saga.Quantity
                })
                .TransitionTo(ReservingStock)
        );

        During(ReservingStock,
            When(StockReserved)
                .Then(ctx =>
                {
                    ctx.Saga.ProductName = ctx.Message.ProductName;
                    ctx.Saga.UnitPrice = ctx.Message.UnitPrice;
                    ctx.Saga.TotalCost = ctx.Message.UnitPrice * ctx.Saga.Quantity;

                    const decimal taxRate = 0.10m;
                    ctx.Saga.TotalTax = ctx.Saga.TotalCost * taxRate;
                    ctx.Saga.TotalRevenue = ctx.Saga.TotalCost - ctx.Saga.TotalTax;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Send(ctx => new Uri("queue:create-purchase-transaction"), ctx => new CreatePurchaseTransaction
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    UserId = ctx.Saga.UserId,
                    ProductId = ctx.Saga.ProductId,
                    ProductName = ctx.Saga.ProductName,
                    Quantity = ctx.Saga.Quantity,
                    TotalCost = ctx.Saga.TotalCost,
                    TotalTax = ctx.Saga.TotalTax,
                    TotalRevenue = ctx.Saga.TotalRevenue,
                    AuthToken = ctx.Saga.AuthToken
                })
                .TransitionTo(CreatingTransaction),
            When(StockReservationFailed)
                .Then(ctx => { ctx.Saga.FailureReason = ctx.Message.Reason; ctx.Saga.UpdatedAt = DateTime.UtcNow; })
                .Publish(ctx => new PurchaseFailed
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    Reason = ctx.Message.Reason
                })
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(CreatingTransaction,
            When(PurchaseTransactionCreated)
                .Then(ctx => { ctx.Saga.TransactionId = ctx.Message.TransactionId; ctx.Saga.UpdatedAt = DateTime.UtcNow; })
                .Send(ctx => new Uri("queue:deduct-stock"), ctx => new DeductStock
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    ProductId = ctx.Saga.ProductId,
                    Quantity = ctx.Saga.Quantity
                })
                .TransitionTo(DeductingStock),
            When(PurchaseTransactionFailed)
                .Then(ctx => { ctx.Saga.FailureReason = ctx.Message.Reason; ctx.Saga.UpdatedAt = DateTime.UtcNow; })
                // Compensation: release the reservation (no stock was deducted yet)
                .Send(ctx => new Uri("queue:release-reservation"), ctx => new ReleaseReservation
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    ProductId = ctx.Saga.ProductId,
                    Quantity = ctx.Saga.Quantity
                })
                .Publish(ctx => new PurchaseFailed
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    Reason = ctx.Message.Reason
                })
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(DeductingStock,
            When(StockDeducted)
                .Then(ctx =>
                {
                    ctx.Saga.RemainingStock = ctx.Message.RemainingStock;
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new PurchaseCompleted
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    ProductId = ctx.Saga.ProductId,
                    ProductName = ctx.Saga.ProductName,
                    QuantityPurchased = ctx.Saga.Quantity,
                    RemainingStock = ctx.Saga.RemainingStock,
                    TotalCost = ctx.Saga.TotalCost
                })
                .TransitionTo(Completed)
                .Finalize(),
            When(StockDeductionFailed)
                .Then(ctx => { ctx.Saga.FailureReason = ctx.Message.Reason; ctx.Saga.UpdatedAt = DateTime.UtcNow; })
                // Compensation: void the financial transaction that was already created
                .Send(ctx => new Uri("queue:void-purchase-transaction"), ctx => new VoidPurchaseTransaction
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    TransactionId = ctx.Saga.TransactionId ?? string.Empty,
                    Reason = $"Stock deduction failed: {ctx.Message.Reason}"
                })
                .Publish(ctx => new PurchaseFailed
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    Reason = ctx.Message.Reason
                })
                .TransitionTo(Faulted)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
