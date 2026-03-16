using MassTransit;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;

namespace ApiGateway.Sagas;

public class PurchaseStateMachine : MassTransitStateMachine<PurchaseState>
{
    // States
    public State ReservingStock { get; private set; } = null!;
    public State CreatingTransaction { get; private set; } = null!;
    public State DeductingStock { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    // Events
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
                })
                .Produce(ctx => ctx.Init<ReserveStock>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.ProductId,
                    ctx.Saga.Quantity
                }))
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
                })
                .Produce(ctx => ctx.Init<CreatePurchaseTransaction>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.UserId,
                    ctx.Saga.ProductId,
                    ctx.Saga.ProductName,
                    ctx.Saga.Quantity,
                    ctx.Saga.TotalCost,
                    ctx.Saga.TotalTax,
                    ctx.Saga.TotalRevenue,
                    ctx.Saga.AuthToken
                }))
                .TransitionTo(CreatingTransaction),
            When(StockReservationFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .Produce(ctx => ctx.Init<PurchaseFailed>(new
                {
                    ctx.Saga.CorrelationId,
                    Reason = ctx.Message.Reason
                }))
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(CreatingTransaction,
            When(PurchaseTransactionCreated)
                .Then(ctx => ctx.Saga.TransactionId = ctx.Message.TransactionId)
                .Produce(ctx => ctx.Init<DeductStock>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.ProductId,
                    ctx.Saga.Quantity
                }))
                .TransitionTo(DeductingStock),
            When(PurchaseTransactionFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                // Compensation: restore the reserved stock
                .Produce(ctx => ctx.Init<RestoreStock>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.ProductId,
                    ctx.Saga.Quantity
                }))
                .Produce(ctx => ctx.Init<PurchaseFailed>(new
                {
                    ctx.Saga.CorrelationId,
                    Reason = ctx.Message.Reason
                }))
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(DeductingStock,
            When(StockDeducted)
                .Then(ctx =>
                {
                    ctx.Saga.RemainingStock = ctx.Message.RemainingStock;
                })
                .Produce(ctx => ctx.Init<PurchaseCompleted>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.ProductId,
                    ctx.Saga.ProductName,
                    QuantityPurchased = ctx.Saga.Quantity,
                    ctx.Saga.RemainingStock,
                    ctx.Saga.TotalCost
                }))
                .TransitionTo(Completed)
                .Finalize(),
            When(StockDeductionFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .Produce(ctx => ctx.Init<PurchaseFailed>(new
                {
                    ctx.Saga.CorrelationId,
                    Reason = ctx.Message.Reason
                }))
                .TransitionTo(Faulted)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
