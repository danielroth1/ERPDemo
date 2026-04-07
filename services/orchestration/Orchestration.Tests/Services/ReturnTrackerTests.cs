using FluentAssertions;
using ERP.Contracts.Events;
using Orchestration.Services;

namespace Orchestration.Tests.Services;

public class ReturnTrackerTests
{
    private readonly ReturnTracker _tracker;

    public ReturnTrackerTests()
    {
        _tracker = new ReturnTracker();
    }

    [Fact]
    public void CreatePending_ShouldReturnCorrelationIdAndTask()
    {
        // Act
        var (correlationId, task) = _tracker.CreatePending(TimeSpan.FromSeconds(30));

        // Assert
        correlationId.Should().NotBeEmpty();
        task.Should().NotBeNull();
        task.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void CreatePending_ShouldGenerateUniqueCorrelationIds()
    {
        // Act
        var (id1, _) = _tracker.CreatePending(TimeSpan.FromSeconds(30));
        var (id2, _) = _tracker.CreatePending(TimeSpan.FromSeconds(30));

        // Assert
        id1.Should().NotBe(id2);
    }

    [Fact]
    public async Task TryComplete_WithValidCorrelationId_ShouldCompleteTask()
    {
        // Arrange
        var (correlationId, task) = _tracker.CreatePending(TimeSpan.FromSeconds(30));
        var result = new ReturnCompleted
        {
            CorrelationId = correlationId,
            ProductName = "Widget",
            RefundAmount = 100,
            NewStock = 75
        };

        // Act
        var completed = _tracker.TryComplete(correlationId, result);

        // Assert
        completed.Should().BeTrue();
        task.IsCompleted.Should().BeTrue();
        var taskResult = await task;
        taskResult.ProductName.Should().Be("Widget");
        taskResult.RefundAmount.Should().Be(100);
    }

    [Fact]
    public void TryComplete_WithInvalidCorrelationId_ShouldReturnFalse()
    {
        // Act
        var result = _tracker.TryComplete(Guid.NewGuid(), new ReturnCompleted());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryFail_WithValidCorrelationId_ShouldFaultTask()
    {
        // Arrange
        var (correlationId, task) = _tracker.CreatePending(TimeSpan.FromSeconds(30));

        // Act
        var failed = _tracker.TryFail(correlationId, "Refund failed");

        // Assert
        failed.Should().BeTrue();
        task.IsFaulted.Should().BeTrue();
    }

    [Fact]
    public void TryFail_WithInvalidCorrelationId_ShouldReturnFalse()
    {
        // Act
        var result = _tracker.TryFail(Guid.NewGuid(), "reason");

        // Assert
        result.Should().BeFalse();
    }
}
