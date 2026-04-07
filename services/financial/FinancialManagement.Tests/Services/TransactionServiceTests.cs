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

public class TransactionServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<ITopicProducer<TransactionCreated>> _transactionCreatedProducerMock;
    private readonly Mock<ILogger<TransactionService>> _loggerMock;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _accountServiceMock = new Mock<IAccountService>();
        _transactionCreatedProducerMock = new Mock<ITopicProducer<TransactionCreated>>();
        _loggerMock = new Mock<ILogger<TransactionService>>();

        _service = new TransactionService(
            _dbContext,
            _accountServiceMock.Object,
            _transactionCreatedProducerMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private void SetupAccountsExist()
    {
        _accountServiceMock
            .Setup(a => a.GetAccountByIdAsync("asset-acct"))
            .ReturnsAsync(new AccountResponse { Id = "asset-acct", Name = "Cash", AccountNumber = "1001" });

        _accountServiceMock
            .Setup(a => a.GetAccountByIdAsync("revenue-acct"))
            .ReturnsAsync(new AccountResponse { Id = "revenue-acct", Name = "Sales Revenue", AccountNumber = "4001" });

        _accountServiceMock
            .Setup(a => a.AdjustBalanceAsync(It.IsAny<string>(), It.IsAny<decimal>()))
            .ReturnsAsync(new AccountResponse());

        // Seed real Account entities (UpdateAccountBalanceAsync queries DB directly)
        if (!_dbContext.Accounts.Any(a => a.Id == "asset-acct"))
        {
            _dbContext.Accounts.Add(new Account
            {
                Id = "asset-acct",
                AccountNumber = "1001",
                Name = "Cash",
                Type = AccountType.Asset,
                Category = AccountCategory.CurrentAssets,
                Currency = "USD"
            });
            _dbContext.Accounts.Add(new Account
            {
                Id = "revenue-acct",
                AccountNumber = "4001",
                Name = "Sales Revenue",
                Type = AccountType.Revenue,
                Category = AccountCategory.OperatingRevenue,
                Currency = "USD"
            });
            _dbContext.SaveChanges();
        }
    }

    private static CreateTransactionRequest CreateValidRequest() => new()
    {
        Date = DateTime.UtcNow,
        Description = "Test Sale",
        Type = "Sale",
        Entries = new List<JournalEntryRequest>
        {
            new() { AccountId = "asset-acct", Debit = 100, Credit = 0 },
            new() { AccountId = "revenue-acct", Debit = 0, Credit = 100 }
        }
    };

    [Fact]
    public async Task CreateTransactionAsync_WithBalancedEntries_ShouldCreateTransaction()
    {
        // Arrange
        SetupAccountsExist();
        var request = CreateValidRequest();

        // Act
        var result = await _service.CreateTransactionAsync(request, "user-1");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.TransactionNumber.Should().StartWith("TXN-");
        result.Description.Should().Be("Test Sale");
        result.Status.Should().Be(TransactionStatus.Posted.ToString());
    }

    [Fact]
    public async Task CreateTransactionAsync_ShouldPublishTransactionCreatedEvent()
    {
        // Arrange
        SetupAccountsExist();
        var request = CreateValidRequest();

        // Act
        await _service.CreateTransactionAsync(request, "user-1");

        // Assert
        _transactionCreatedProducerMock.Verify(
            p => p.Produce(It.IsAny<TransactionCreated>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithUnbalancedEntries_ShouldThrowException()
    {
        // Arrange
        SetupAccountsExist();
        var request = new CreateTransactionRequest
        {
            Date = DateTime.UtcNow,
            Description = "Unbalanced",
            Type = "Sale",
            Entries = new List<JournalEntryRequest>
            {
                new() { AccountId = "asset-acct", Debit = 100, Credit = 0 },
                new() { AccountId = "revenue-acct", Debit = 0, Credit = 50 } // Unbalanced!
            }
        };

        // Act
        var act = () => _service.CreateTransactionAsync(request, "user-1");

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetTransactionByIdAsync_WithExistingId_ShouldReturnTransaction()
    {
        // Arrange
        SetupAccountsExist();
        var created = await _service.CreateTransactionAsync(CreateValidRequest(), "user-1");

        // Act
        var result = await _service.GetTransactionByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetTransactionByIdAsync("non-existing");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllTransactionsAsync_ShouldReturnAll()
    {
        // Arrange
        SetupAccountsExist();
        await _service.CreateTransactionAsync(CreateValidRequest(), "user-1");
        var secondRequest = CreateValidRequest();
        secondRequest.Description = "Second";
        await _service.CreateTransactionAsync(secondRequest, "user-1");

        // Act
        var result = await _service.GetAllTransactionsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task VoidTransactionAsync_ShouldSetStatusToVoided()
    {
        // Arrange
        SetupAccountsExist();
        var created = await _service.CreateTransactionAsync(CreateValidRequest(), "user-1");

        // Act
        var result = await _service.VoidTransactionAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(TransactionStatus.Voided.ToString());
    }

    [Fact]
    public async Task VoidTransactionAsync_ShouldReverseAccountBalances()
    {
        // Arrange
        SetupAccountsExist();
        var created = await _service.CreateTransactionAsync(CreateValidRequest(), "user-1");

        // Capture balances after creation
        var assetBefore = (await _dbContext.Accounts.FindAsync("asset-acct"))!.Balance;
        var revenueBefore = (await _dbContext.Accounts.FindAsync("revenue-acct"))!.Balance;

        // Act
        await _service.VoidTransactionAsync(created.Id);

        // Assert — balances should be reversed (net zero)
        var assetAfter = (await _dbContext.Accounts.FindAsync("asset-acct"))!.Balance;
        var revenueAfter = (await _dbContext.Accounts.FindAsync("revenue-acct"))!.Balance;
        assetAfter.Should().Be(0);
        revenueAfter.Should().Be(0);
    }

    [Fact]
    public async Task GetTransactionsByDateRangeAsync_ShouldFilterCorrectly()
    {
        // Arrange
        SetupAccountsExist();
        await _service.CreateTransactionAsync(CreateValidRequest(), "user-1");

        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow.AddDays(1);

        // Act
        var result = await _service.GetTransactionsByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTransactionsByDateRangeAsync_OutOfRange_ShouldReturnEmpty()
    {
        // Arrange
        SetupAccountsExist();
        await _service.CreateTransactionAsync(CreateValidRequest(), "user-1");

        var startDate = DateTime.UtcNow.AddDays(10);
        var endDate = DateTime.UtcNow.AddDays(20);

        // Act
        var result = await _service.GetTransactionsByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().BeEmpty();
    }
}
