using MassTransit;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;

namespace Orchestration.Sagas;

public class ReturnStateMachine : MassTransitStateMachine<ReturnState>
{
    // States
    public State CreatingRefund { get; private set; } = null!;
    public State RestoringStock { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Faulted { get; private set; } = null!;

    // Events (all arrive via RabbitMQ)
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
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Send(ctx => new Uri("queue:create-refund-transaction"), ctx => new CreateRefundTransaction
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    UserId = ctx.Saga.UserId,
                    ProductId = ctx.Saga.ProductId,
                    ProductName = "",
                    Quantity = ctx.Saga.Quantity,
                    RefundAmount = 0m,
                    AuthToken = ctx.Saga.AuthToken
                })
                .TransitionTo(CreatingRefund)
        );

        During(CreatingRefund,
            When(RefundTransactionCreated)
                .Then(ctx => { ctx.Saga.TransactionId = ctx.Message.TransactionId; ctx.Saga.UpdatedAt = DateTime.UtcNow; })
                .Send(ctx => new Uri("queue:restore-stock"), ctx => new RestoreStock
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    ProductId = ctx.Saga.ProductId,
                    Quantity = ctx.Saga.Quantity
                })
                .TransitionTo(RestoringStock),
            When(RefundTransactionFailed)
                .Then(ctx => { ctx.Saga.FailureReason = ctx.Message.Reason; ctx.Saga.UpdatedAt = DateTime.UtcNow; })
                .Publish(ctx => new ReturnFailed
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    Reason = ctx.Message.Reason
                })
                .TransitionTo(Faulted)
                .Finalize()
        );

        During(RestoringStock,
            When(StockRestored)
                .Then(ctx =>
                {
                    ctx.Saga.NewStock = ctx.Message.NewStock;
                    ctx.Saga.ProductName = "";
                    ctx.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new ReturnCompleted
                {
                    CorrelationId = ctx.Saga.CorrelationId,
                    ProductId = ctx.Saga.ProductId,
                    ProductName = ctx.Saga.ProductName,
                    QuantityReturned = ctx.Saga.Quantity,
                    NewStock = ctx.Saga.NewStock,
                    RefundAmount = ctx.Saga.RefundAmount
                })
                .TransitionTo(Completed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
