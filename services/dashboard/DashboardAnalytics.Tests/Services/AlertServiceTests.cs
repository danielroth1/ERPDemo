using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Models;
using DashboardAnalytics.Services;
using DashboardAnalytics.Tests.Helpers;

namespace DashboardAnalytics.Tests.Services;

public class AlertServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<AlertService>> _loggerMock;
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _loggerMock = new Mock<ILogger<AlertService>>();
        _service = new AlertService(_dbContext, _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private async Task<Alert> SeedAlert(
        string title = "Test Alert",
        AlertSeverity severity = AlertSeverity.Warning,
        bool isRead = false)
    {
        var alert = new Alert
        {
            Title = title,
            Message = $"{title} message",
            Severity = severity,
            Source = "UnitTest",
            IsRead = isRead,
            Data = new Dictionary<string, object> { ["key"] = "value" }
        };
        _dbContext.Alerts.Add(alert);
        await _dbContext.SaveChangesAsync();
        return alert;
    }

    [Fact]
    public async Task GetAlertByIdAsync_WithExistingId_ShouldReturnAlert()
    {
        // Arrange
        var alert = await SeedAlert();

        // Act
        var result = await _service.GetAlertByIdAsync(alert.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(alert.Id);
        result.Title.Should().Be("Test Alert");
    }

    [Fact]
    public async Task GetAlertByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetAlertByIdAsync("non-existing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAlertsAsync_ShouldReturnAllAlerts()
    {
        // Arrange
        await SeedAlert("Alert 1");
        await SeedAlert("Alert 2");
        await SeedAlert("Alert 3");

        // Act
        var result = await _service.GetAllAlertsAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetUnreadAlertsAsync_ShouldReturnOnlyUnreadAlerts()
    {
        // Arrange
        await SeedAlert("Unread Alert", isRead: false);
        await SeedAlert("Read Alert", isRead: true);

        // Act
        var result = await _service.GetUnreadAlertsAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Unread Alert");
    }

    [Fact]
    public async Task MarkAsReadAsync_ShouldSetIsReadToTrue()
    {
        // Arrange
        var alert = await SeedAlert();

        // Act
        var result = await _service.MarkAsReadAsync(alert.Id);

        // Assert
        result.Should().BeTrue();
        var updated = await _dbContext.Alerts.FindAsync(alert.Id);
        updated!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.MarkAsReadAsync("non-existing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAlertAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var alert = await SeedAlert();

        // Act
        var result = await _service.DeleteAlertAsync(alert.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await _dbContext.Alerts.FindAsync(alert.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAlertAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteAlertAsync("non-existing");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(AlertSeverity.Info)]
    [InlineData(AlertSeverity.Warning)]
    [InlineData(AlertSeverity.Error)]
    [InlineData(AlertSeverity.Critical)]
    public async Task Alert_AllSeverities_ShouldBeSupported(AlertSeverity severity)
    {
        // Arrange
        var alert = await SeedAlert(severity: severity);

        // Act
        var result = await _service.GetAlertByIdAsync(alert.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Severity.Should().Be(severity);
    }
}
