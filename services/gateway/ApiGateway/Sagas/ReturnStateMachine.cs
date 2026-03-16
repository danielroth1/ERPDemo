using MassTransit;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;

namespace ApiGateway.Sagas;

public class ReturnStateMachine : MassTransitStateMachine<ReturnState>
{
    // States
    public State CreatingRefund { get; private set; } = null!;
    public State RestoringStock { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    // Events
    public Event<SubmitReturn> SubmitReturn { get; private set; } = null!;
    public Event<RefundTransactionCreated> RefundTransactionCreated { get; private set; } = null!;
    public Event<RefundTransactionFailed> RefundTransactionFailed { get; private set; } = null!;
    public Event<StockRestored> StockRestored { get; private set; } = null!;

    public ReturnStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => SubmitReturn, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => RefundTransactionCreated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => RefundTransactionFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockRestored, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Initially(
            When(SubmitReturn)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.ProductId = ctx.Message.ProductId;
                    ctx.Saga.Quantity = ctx.Message.Quantity;
                    ctx.Saga.AuthToken = ctx.Message.AuthToken;
                })
                .Produce(ctx => ctx.Init<CreateRefundTransaction>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.UserId,
                    ctx.Saga.ProductId,
                    ProductName = "", // Financial will look up the product
                    ctx.Saga.Quantity,
                    RefundAmount = 0m, // Financial will calculate from accounts
                    ctx.Saga.AuthToken
                }))
                .TransitionTo(CreatingRefund)
        );

        During(CreatingRefund,
            When(RefundTransactionCreated)
                .Then(ctx => ctx.Saga.TransactionId = ctx.Message.TransactionId)
                .Produce(ctx => ctx.Init<RestoreStock>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.ProductId,
                    ctx.Saga.Quantity
                }))
                .TransitionTo(RestoringStock),
            When(RefundTransactionFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .Produce(ctx => ctx.Init<ReturnFailed>(new
                {
                    ctx.Saga.CorrelationId,
                    Reason = ctx.Message.Reason
                }))
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(RestoringStock,
            When(StockRestored)
                .Then(ctx =>
                {
                    ctx.Saga.NewStock = ctx.Message.NewStock;
                    ctx.Saga.ProductName = ""; // Will be filled from event
                })
                .Produce(ctx => ctx.Init<ReturnCompleted>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.ProductId,
                    ProductName = ctx.Saga.ProductName,
                    QuantityReturned = ctx.Saga.Quantity,
                    ctx.Saga.NewStock,
                    ctx.Saga.RefundAmount
                }))
                .TransitionTo(Completed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
