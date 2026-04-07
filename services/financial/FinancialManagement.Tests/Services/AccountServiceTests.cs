using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using FinancialManagement.Infrastructure;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Services;
using FinancialManagement.Tests.Helpers;

namespace FinancialManagement.Tests.Services;

public class AccountServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<AccountService>> _loggerMock;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _loggerMock = new Mock<ILogger<AccountService>>();
        _service = new AccountService(_dbContext, _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private static CreateAccountRequest CreateValidRequest(
        string name = "Test Account",
        string type = "Asset",
        string category = "CurrentAssets") => new()
    {
        Name = name,
        Type = type,
        Category = category,
        Currency = "USD",
        Description = "Test account"
    };

    [Fact]
    public async Task CreateAccountAsync_WithValidRequest_ShouldCreateAccount()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = await _service.CreateAccountAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("Test Account");
        result.AccountNumber.Should().StartWith("1"); // Asset accounts start with 1
        result.Balance.Should().Be(0);
        result.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("Asset", "1")]
    [InlineData("Liability", "2")]
    [InlineData("Equity", "3")]
    [InlineData("Revenue", "4")]
    [InlineData("Expense", "5")]
    public async Task CreateAccountAsync_ShouldGenerateCorrectAccountNumber(string type, string expectedPrefix)
    {
        // Arrange
        var request = CreateValidRequest(type: type);

        // Act
        var result = await _service.CreateAccountAsync(request);

        // Assert
        result.AccountNumber.Should().StartWith(expectedPrefix);
    }

    [Fact]
    public async Task CreateUserAccountsAsync_ShouldCreateAssetAndExpenseAccounts()
    {
        // Act
        var (asset, expense) = await _service.CreateUserAccountsAsync("user-1", "John Doe");

        // Assert
        asset.Should().NotBeNull();
        asset.Type.Should().Be("Asset");
        asset.UserId.Should().Be("user-1");

        expense.Should().NotBeNull();
        expense.Type.Should().Be("Expense");
        expense.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetAccountByIdAsync_WithExistingId_ShouldReturnAccount()
    {
        // Arrange
        var created = await _service.CreateAccountAsync(CreateValidRequest());

        // Act
        var result = await _service.GetAccountByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetAccountByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetAccountByIdAsync("non-existing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAccountByNumberAsync_ShouldReturnCorrectAccount()
    {
        // Arrange
        var created = await _service.CreateAccountAsync(CreateValidRequest());

        // Act
        var result = await _service.GetAccountByNumberAsync(created.AccountNumber);

        // Assert
        result.Should().NotBeNull();
        result!.AccountNumber.Should().Be(created.AccountNumber);
    }

    [Fact]
    public async Task GetAccountByUserIdAsync_ShouldReturnUserAccount()
    {
        // Arrange
        await _service.CreateUserAccountsAsync("user-1", "John Doe");

        // Act
        var result = await _service.GetAccountByUserIdAsync("user-1");

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetAllAccountsAsync_ShouldReturnAllAccounts()
    {
        // Arrange
        await _service.CreateAccountAsync(CreateValidRequest("Account 1"));
        await _service.CreateAccountAsync(CreateValidRequest("Account 2", "Liability", "CurrentLiabilities"));

        // Act
        var result = await _service.GetAllAccountsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccountsByTypeAsync_ShouldFilterByType()
    {
        // Arrange
        await _service.CreateAccountAsync(CreateValidRequest("Asset Account", "Asset"));
        await _service.CreateAccountAsync(CreateValidRequest("Expense Account", "Expense", "OperatingExpenses"));

        // Act
        var result = await _service.GetAccountsByTypeAsync(AccountType.Asset);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Asset Account");
    }

    [Fact]
    public async Task UpdateAccountAsync_WithValidData_ShouldUpdateAccount()
    {
        // Arrange
        var created = await _service.CreateAccountAsync(CreateValidRequest());
        var updateRequest = new UpdateAccountRequest { Name = "Updated Name", Description = "Updated desc" };

        // Act
        var result = await _service.UpdateAccountAsync(created.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteAccountAsync_ShouldSoftDelete()
    {
        // Arrange
        var created = await _service.CreateAccountAsync(CreateValidRequest());

        // Act
        var result = await _service.DeleteAccountAsync(created.Id);

        // Assert
        result.Should().BeTrue();
        var account = await _dbContext.Accounts.FindAsync(created.Id);
        account.Should().NotBeNull();
        account!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AdjustBalanceAsync_ShouldUpdateBalance()
    {
        // Arrange
        var created = await _service.CreateAccountAsync(CreateValidRequest());

        // Act
        var result = await _service.AdjustBalanceAsync(created.Id, 500.00m);

        // Assert
        result.Should().NotBeNull();
        result!.Balance.Should().Be(500.00m);
    }

    [Fact]
    public async Task AdjustBalanceAsync_WithNegativeAmount_ShouldDecreaseBalance()
    {
        // Arrange
        var created = await _service.CreateAccountAsync(CreateValidRequest());
        await _service.AdjustBalanceAsync(created.Id, 1000.00m);

        // Act
        var result = await _service.AdjustBalanceAsync(created.Id, -300.00m);

        // Assert
        result.Should().NotBeNull();
        result!.Balance.Should().Be(700.00m);
    }

    [Fact]
    public async Task GetAccountBalanceAsync_ShouldReturnCurrentBalance()
    {
        // Arrange
        var created = await _service.CreateAccountAsync(CreateValidRequest());
        await _service.AdjustBalanceAsync(created.Id, 250.00m);

        // Act
        var balance = await _service.GetAccountBalanceAsync(created.Id);

        // Assert
        balance.Should().Be(250.00m);
    }
}
