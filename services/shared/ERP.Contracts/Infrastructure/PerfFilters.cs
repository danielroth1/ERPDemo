using System.Diagnostics;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ERP.Contracts.Infrastructure;

/// <summary>
/// Logs how long a message sat in the broker before being consumed (message age)
/// and how long the consumer took to execute.
/// </summary>
public class PerfConsumeFilter<T> : IFilter<ConsumeContext<T>> where T : class
{
    private readonly ILogger<PerfConsumeFilter<T>> _logger;

    public PerfConsumeFilter(ILogger<PerfConsumeFilter<T>> logger) => _logger = logger;

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var messageType = typeof(T).Name;

        // Message age: time between SentTime (when the message was produced) and now
        var sentTime = context.SentTime;
        var messageAgeMs = sentTime.HasValue
            ? (long)(DateTime.UtcNow - sentTime.Value).TotalMilliseconds
            : -1;

        _logger.LogInformation("[PERF] {MessageType} received, messageAge={MessageAgeMs}ms, messageId={MessageId}",
            messageType, messageAgeMs, context.MessageId);

        var sw = Stopwatch.StartNew();
        try
        {
            await next.Send(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("[PERF] {MessageType} consumed in {ElapsedMs}ms, messageId={MessageId}",
                messageType, sw.ElapsedMilliseconds, context.MessageId);
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("perf-consume");
}

/// <summary>
/// Logs the time taken for Publish calls (writing to outbox or direct to broker).
/// </summary>
public class PerfPublishFilter<T> : IFilter<PublishContext<T>> where T : class
{
    private readonly ILogger<PerfPublishFilter<T>> _logger;

    public PerfPublishFilter(ILogger<PerfPublishFilter<T>> logger) => _logger = logger;

    public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var messageType = typeof(T).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            await next.Send(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("[PERF] Publish<{MessageType}> took {ElapsedMs}ms, messageId={MessageId}",
                messageType, sw.ElapsedMilliseconds, context.MessageId);
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("perf-publish");
}

/// <summary>
/// Logs the time taken for Send calls (writing to outbox or direct to broker).
/// </summary>
public class PerfSendFilter<T> : IFilter<SendContext<T>> where T : class
{
    private readonly ILogger<PerfSendFilter<T>> _logger;

    public PerfSendFilter(ILogger<PerfSendFilter<T>> logger) => _logger = logger;

    public async Task Send(SendContext<T> context, IPipe<SendContext<T>> next)
    {
        var messageType = typeof(T).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            await next.Send(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("[PERF] Send<{MessageType}> took {ElapsedMs}ms, messageId={MessageId}",
                messageType, sw.ElapsedMilliseconds, context.MessageId);
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("perf-send");
}
