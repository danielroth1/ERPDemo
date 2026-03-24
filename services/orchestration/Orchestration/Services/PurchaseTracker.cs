using System.Collections.Concurrent;
using ERP.Contracts.Events;

namespace Orchestration.Services;

public class PurchaseTracker
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<PurchaseCompleted>> _pending = new();

    public (Guid CorrelationId, Task<PurchaseCompleted> Task) CreatePending(TimeSpan timeout)
    {
        var correlationId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<PurchaseCompleted>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[correlationId] = tcs;

        var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() =>
        {
            if (_pending.TryRemove(correlationId, out var removed))
            {
                removed.TrySetCanceled();
            }
            cts.Dispose();
        });

        return (correlationId, tcs.Task);
    }

    public bool TryComplete(Guid correlationId, PurchaseCompleted result)
    {
        if (_pending.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetResult(result);
        }
        return false;
    }

    public bool TryFail(Guid correlationId, string reason)
    {
        if (_pending.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetException(new InvalidOperationException(reason));
        }
        return false;
    }
}

public class ReturnTracker
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ReturnCompleted>> _pending = new();

    public (Guid CorrelationId, Task<ReturnCompleted> Task) CreatePending(TimeSpan timeout)
    {
        var correlationId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<ReturnCompleted>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[correlationId] = tcs;

        var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() =>
        {
            if (_pending.TryRemove(correlationId, out var removed))
            {
                removed.TrySetCanceled();
            }
            cts.Dispose();
        });

        return (correlationId, tcs.Task);
    }

    public bool TryComplete(Guid correlationId, ReturnCompleted result)
    {
        if (_pending.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetResult(result);
        }
        return false;
    }

    public bool TryFail(Guid correlationId, string reason)
    {
        if (_pending.TryRemove(correlationId, out var tcs))
        {
            return tcs.TrySetException(new InvalidOperationException(reason));
        }
        return false;
    }
}
