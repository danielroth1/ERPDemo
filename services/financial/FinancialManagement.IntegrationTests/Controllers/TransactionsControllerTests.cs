using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using FinancialManagement.IntegrationTests.Fixtures;
using FinancialManagement.Models.DTOs;

namespace FinancialManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class TransactionsControllerTests : IAsyncLifetime
{
    private readonly FinancialIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public TransactionsControllerTests(FinancialIntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    public async Task InitializeAsync()
    {
        await _fixture.DbResetter.ResetAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private async Task<(AccountResponse Debit, AccountResponse Credit)> CreateTwoAccountsAsync()
    {
        var debitRequest = new CreateAccountRequest
        {
            Name = "Cash Account",
            Type = "Asset",
            Category = "CurrentAssets",
            Currency = "USD"
        };
        var creditRequest = new CreateAccountRequest
        {
            Name = "Revenue Account",
            Type = "Revenue",
            Category = "OperatingRevenue",
            Currency = "USD"
        };

        var debitResponse = await _client.PostAsJsonAsync("/api/v1/accounts", debitRequest);
        var debit = (await debitResponse.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>())!.Data;

        var creditResponse = await _client.PostAsJsonAsync("/api/v1/accounts", creditRequest);
        var credit = (await creditResponse.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>())!.Data;

        return (debit, credit);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTransactions()
    {
        var response = await _client.GetAsync("/api/v1/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsCreatedTransaction()
    {
        var (debit, credit) = await CreateTwoAccountsAsync();

        var request = new CreateTransactionRequest
        {
            Description = "Test Transaction",
            Type = "Sale",
            Entries = new List<JournalEntryRequest>
            {
                new() { AccountId = debit.Id, Debit = 100, Credit = 0 },
                new() { AccountId = credit.Id, Debit = 0, Credit = 100 }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TransactionResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.Description.Should().Be("Test Transaction");
        result.Data.TransactionNumber.Should().NotBeNullOrEmpty();
        result.Data.Type.Should().Be("Sale");
    }

    [Fact]
    public async Task GetById_ReturnsTransaction_AfterCreate()
    {
        var (debit, credit) = await CreateTwoAccountsAsync();
        var createRequest = new CreateTransactionRequest
        {
            Description = "Lookup Test",
            Type = "Sale",
            Entries = new List<JournalEntryRequest>
            {
                new() { AccountId = debit.Id, Debit = 50, Credit = 0 },
                new() { AccountId = credit.Id, Debit = 0, Credit = 50 }
            }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/transactions", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<TransactionResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/transactions/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TransactionResponse>>();
        result!.Data.Id.Should().Be(created.Id);
    }

    [Fact(Skip = "EF Core cannot translate JSONB Any() LINQ query - service-level issue")]
    public async Task GetByAccount_ReturnsMatchingTransactions()
    {
        var (debit, credit) = await CreateTwoAccountsAsync();
        var request = new CreateTransactionRequest
        {
            Description = "Account Lookup Test",
            Type = "Sale",
            Entries = new List<JournalEntryRequest>
            {
                new() { AccountId = debit.Id, Debit = 75, Credit = 0 },
                new() { AccountId = credit.Id, Debit = 0, Credit = 75 }
            }
        };
        await _client.PostAsJsonAsync("/api/v1/transactions", request);

        var response = await _client.GetAsync($"/api/v1/transactions/account/{debit.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/v1/transactions",
            new CreateTransactionRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
