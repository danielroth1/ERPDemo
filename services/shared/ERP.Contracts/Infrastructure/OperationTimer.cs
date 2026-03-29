using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ERP.Contracts.Infrastructure;

/// <summary>
/// Lightweight operation timer for benchmarking individual steps within a workflow.
/// Usage: using var step = timer.Step("StepName"); // logs duration on dispose
/// </summary>
public sealed class OperationTimer
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly Stopwatch _totalStopwatch = Stopwatch.StartNew();
    private readonly List<StepResult> _steps = new();

    public OperationTimer(ILogger logger, string operationName)
    {
        _logger = logger;
        _operationName = operationName;
    }

    /// <summary>
    /// Start timing a named step. Dispose the result to record the elapsed time.
    /// </summary>
    public StepHandle Step(string stepName) => new(this, stepName);

    private void RecordStep(string stepName, long elapsedMs)
    {
        _steps.Add(new StepResult(stepName, elapsedMs));
        _logger.LogInformation("[PERF] {Operation}.{Step} took {ElapsedMs}ms",
            _operationName, stepName, elapsedMs);
    }

    /// <summary>
    /// Log a summary of all recorded steps and total elapsed time.
    /// </summary>
    public void LogSummary()
    {
        _totalStopwatch.Stop();
        var stepDetails = string.Join(", ", _steps.Select(s => $"{s.Name}={s.ElapsedMs}ms"));
        _logger.LogInformation("[PERF] {Operation} total={TotalMs}ms steps=[{Steps}]",
            _operationName, _totalStopwatch.ElapsedMilliseconds, stepDetails);
    }

    private readonly record struct StepResult(string Name, long ElapsedMs);

    public readonly struct StepHandle : IDisposable
    {
        private readonly OperationTimer _timer;
        private readonly string _stepName;
        private readonly long _startTicks;

        internal StepHandle(OperationTimer timer, string stepName)
        {
            _timer = timer;
            _stepName = stepName;
            _startTicks = Stopwatch.GetTimestamp();
        }

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTicks);
            _timer.RecordStep(_stepName, (long)elapsed.TotalMilliseconds);
        }
    }
}
