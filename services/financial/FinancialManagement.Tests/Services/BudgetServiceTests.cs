using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using FinancialManagement.Infrastructure;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Services;
using FinancialManagement.Tests.Helpers;
using ERP.Contracts.Events.Domain;

namespace FinancialManagement.Tests.Services;

public class BudgetServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<ITopicProducer<BudgetExceeded>> _budgetExceededProducerMock;
    private readonly Mock<ILogger<BudgetService>> _loggerMock;
    private readonly BudgetService _service;

    public BudgetServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _accountServiceMock = new Mock<IAccountService>();
        _budgetExceededProducerMock = new Mock<ITopicProducer<BudgetExceeded>>();
        _loggerMock = new Mock<ILogger<BudgetService>>();

        _service = new BudgetService(
            _dbContext,
            _accountServiceMock.Object,
            _budgetExceededProducerMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private void SetupAccountExists(string accountId = "account-1")
    {
        _accountServiceMock
            .Setup(a => a.GetAccountByIdAsync(accountId))
            .ReturnsAsync(new AccountResponse { Id = accountId, Name = "Test Account" });
    }

    private static CreateBudgetRequest CreateValidRequest(string accountId = "account-1") => new()
    {
        Name = "Q1 Budget",
        AccountId = accountId,
        Period = "Quarterly",
        StartDate = DateTime.UtcNow.AddDays(-30),
        EndDate = DateTime.UtcNow.AddDays(60),
        Amount = 10000
    };

    [Fact]
    public async Task CreateBudgetAsync_WithValidRequest_ShouldCreateBudget()
    {
        // Arrange
        SetupAccountExists();
        var request = CreateValidRequest();

        // Act
        var result = await _service.CreateBudgetAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Q1 Budget");
        result.Amount.Should().Be(10000);
        result.Spent.Should().Be(0);
        result.Remaining.Should().Be(10000);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetBudgetByIdAsync_WithExistingId_ShouldReturnBudget()
    {
        // Arrange
        SetupAccountExists();
        var created = await _service.CreateBudgetAsync(CreateValidRequest());

        // Act
        var result = await _service.GetBudgetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetAllBudgetsAsync_ShouldReturnAllBudgets()
    {
        // Arrange
        SetupAccountExists();
        await _service.CreateBudgetAsync(CreateValidRequest());
        var secondRequest = CreateValidRequest();
        secondRequest.Name = "Q2 Budget";
        await _service.CreateBudgetAsync(secondRequest);

        // Act
        var result = await _service.GetAllBudgetsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveBudgetsAsync_ShouldReturnOnlyActiveBudgets()
    {
        // Arrange
        SetupAccountExists();
        await _service.CreateBudgetAsync(CreateValidRequest());

        // Add an inactive budget directly
        _dbContext.Budgets.Add(new Budget
        {
            Name = "Inactive Budget",
            AccountId = "account-1",
            Period = BudgetPeriod.Monthly,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Amount = 5000,
            Remaining = 5000,
            IsActive = false
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetActiveBudgetsAsync();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetBudgetsByAccountAsync_ShouldFilterByAccountId()
    {
        // Arrange
        SetupAccountExists("acct-1");
        SetupAccountExists("acct-2");
        await _service.CreateBudgetAsync(CreateValidRequest("acct-1"));
        var otherRequest = CreateValidRequest("acct-2");
        otherRequest.Name = "Other Budget";
        await _service.CreateBudgetAsync(otherRequest);

        // Act
        var result = await _service.GetBudgetsByAccountAsync("acct-1");

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Q1 Budget");
    }

    [Fact]
    public async Task UpdateBudgetAsync_WithValidData_ShouldUpdateBudget()
    {
        // Arrange
        SetupAccountExists();
        var created = await _service.CreateBudgetAsync(CreateValidRequest());
        var updateRequest = new UpdateBudgetRequest { Name = "Updated Budget", Amount = 15000 };

        // Act
        var result = await _service.UpdateBudgetAsync(created.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Budget");
        result.Amount.Should().Be(15000);
    }

    [Fact]
    public async Task DeleteBudgetAsync_ShouldSoftDelete()
    {
        // Arrange
        SetupAccountExists();
        var created = await _service.CreateBudgetAsync(CreateValidRequest());

        // Act
        var result = await _service.DeleteBudgetAsync(created.Id);

        // Assert
        result.Should().BeTrue();
        var budget = await _dbContext.Budgets.FindAsync(created.Id);
        budget.Should().NotBeNull();
        budget!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBudgetSpendingAsync_ShouldUpdateSpentAndRemaining()
    {
        // Arrange
        SetupAccountExists();
        var created = await _service.CreateBudgetAsync(CreateValidRequest());

        // Act
        await _service.UpdateBudgetSpendingAsync("account-1", 3000);

        // Assert
        var budget = await _dbContext.Budgets.FindAsync(created.Id);
        budget.Should().NotBeNull();
        budget!.Spent.Should().Be(3000);
        budget.Remaining.Should().Be(7000);
    }

    [Fact]
    public async Task UpdateBudgetSpendingAsync_WhenExceedingBudget_ShouldPublishEvent()
    {
        // Arrange
        SetupAccountExists();
        await _service.CreateBudgetAsync(CreateValidRequest());

        // Act
        await _service.UpdateBudgetSpendingAsync("account-1", 11000);

        // Assert
        _budgetExceededProducerMock.Verify(
            p => p.Produce(It.IsAny<BudgetExceeded>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
