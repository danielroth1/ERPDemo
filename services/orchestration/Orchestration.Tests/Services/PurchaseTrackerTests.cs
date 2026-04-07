using FluentAssertions;
using ERP.Contracts.Events;
using Orchestration.Services;

namespace Orchestration.Tests.Services;

public class PurchaseTrackerTests
{
    private readonly PurchaseTracker _tracker;

    public PurchaseTrackerTests()
    {
        _tracker = new PurchaseTracker();
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
        var result = new PurchaseCompleted
        {
            CorrelationId = correlationId,
            ProductName = "Widget",
            TotalCost = 100,
            RemainingStock = 50
        };

        // Act
        var completed = _tracker.TryComplete(correlationId, result);

        // Assert
        completed.Should().BeTrue();
        task.IsCompleted.Should().BeTrue();
        var taskResult = await task;
        taskResult.ProductName.Should().Be("Widget");
    }

    [Fact]
    public void TryComplete_WithInvalidCorrelationId_ShouldReturnFalse()
    {
        // Act
        var result = _tracker.TryComplete(Guid.NewGuid(), new PurchaseCompleted());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryFail_WithValidCorrelationId_ShouldFaultTask()
    {
        // Arrange
        var (correlationId, task) = _tracker.CreatePending(TimeSpan.FromSeconds(30));

        // Act
        var failed = _tracker.TryFail(correlationId, "Out of stock");

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

    [Fact]
    public async Task CreatePending_WithTimeout_ShouldCancelAfterTimeout()
    {
        // Arrange
        var (_, task) = _tracker.CreatePending(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => task);
    }
}
