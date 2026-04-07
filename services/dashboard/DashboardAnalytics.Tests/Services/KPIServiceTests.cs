using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;
using DashboardAnalytics.Tests.Helpers;

namespace DashboardAnalytics.Tests.Services;

public class KPIServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<KPIService>> _loggerMock;
    private readonly KPIService _service;

    public KPIServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _loggerMock = new Mock<ILogger<KPIService>>();
        _service = new KPIService(_dbContext, _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private static CreateKPIRequest CreateValidRequest(string name = "Revenue Growth") =>
        new(name, "Monthly revenue growth rate", 100);

    [Fact]
    public async Task CreateKPIAsync_WithValidRequest_ShouldCreateKPI()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = await _service.CreateKPIAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Revenue Growth");
        result.CurrentValue.Should().Be(0);
        result.TargetValue.Should().Be(100);
        result.PreviousValue.Should().Be(0);
    }

    [Fact]
    public async Task GetKPIByIdAsync_WithExistingId_ShouldReturnKPI()
    {
        // Arrange
        var created = await _service.CreateKPIAsync(CreateValidRequest());

        // Act
        var result = await _service.GetKPIByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetKPIByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetKPIByIdAsync("non-existing-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllKPIsAsync_ShouldReturnAllKPIs()
    {
        // Arrange
        await _service.CreateKPIAsync(CreateValidRequest("Revenue Growth"));
        await _service.CreateKPIAsync(CreateValidRequest("Customer Satisfaction"));

        // Act
        var result = await _service.GetAllKPIsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateKPIAsync_WithValidData_ShouldUpdateKPI()
    {
        // Arrange
        var created = await _service.CreateKPIAsync(CreateValidRequest());
        var updateRequest = new UpdateKPIRequest(90, 100);

        // Act
        var result = await _service.UpdateKPIAsync(created.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.CurrentValue.Should().Be(90);
    }

    [Fact]
    public async Task UpdateKPIAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var updateRequest = new UpdateKPIRequest(90, null);

        // Act
        var result = await _service.UpdateKPIAsync("non-existing", updateRequest);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteKPIAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var created = await _service.CreateKPIAsync(CreateValidRequest());

        // Act
        var result = await _service.DeleteKPIAsync(created.Id);

        // Assert
        result.Should().BeTrue();
        var deleted = await _service.GetKPIByIdAsync(created.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteKPIAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteKPIAsync("non-existing");

        // Assert
        result.Should().BeFalse();
    }
}
