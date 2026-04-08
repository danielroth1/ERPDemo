using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using FinancialManagement.IntegrationTests.Fixtures;
using FinancialManagement.Models.DTOs;

namespace FinancialManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class BudgetsControllerTests : IAsyncLifetime
{
    private readonly FinancialIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public BudgetsControllerTests(FinancialIntegrationFixture fixture)
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

    private async Task<AccountResponse> CreateAccountAsync()
    {
        var request = new CreateAccountRequest
        {
            Name = $"Budget Account {Guid.NewGuid():N}",
            Type = "Expense",
            Category = "OperatingExpenses",
            Currency = "USD"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/accounts", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>())!.Data;
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoBudgets()
    {
        var response = await _client.GetAsync("/api/v1/budgets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsCreatedBudget()
    {
        var account = await CreateAccountAsync();
        var request = new CreateBudgetRequest
        {
            Name = "Q1 Budget",
            AccountId = account.Id,
            Period = "Monthly",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Amount = 5000
        };

        var response = await _client.PostAsJsonAsync("/api/v1/budgets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BudgetResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.Name.Should().Be("Q1 Budget");
        result.Data.Amount.Should().Be(5000);
        result.Data.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_ReturnsBudget_AfterCreate()
    {
        var account = await CreateAccountAsync();
        var createRequest = new CreateBudgetRequest
        {
            Name = "Lookup Budget",
            AccountId = account.Id,
            Period = "Monthly",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Amount = 3000
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/budgets", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<BudgetResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/budgets/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<BudgetResponse>>();
        result!.Data.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Update_ModifiesBudget()
    {
        var account = await CreateAccountAsync();
        var createRequest = new CreateBudgetRequest
        {
            Name = "Original Budget",
            AccountId = account.Id,
            Period = "Monthly",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Amount = 2000
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/budgets", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<BudgetResponse>>())!.Data;

        var updateRequest = new UpdateBudgetRequest { Name = "Updated Budget", Amount = 8000 };
        var response = await _client.PutAsJsonAsync($"/api/v1/budgets/{created.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/budgets/{created.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<BudgetResponse>>();
        result!.Data.Name.Should().Be("Updated Budget");
        result.Data.Amount.Should().Be(8000);
    }

    [Fact]
    public async Task Delete_RemovesBudget()
    {
        var account = await CreateAccountAsync();
        var createRequest = new CreateBudgetRequest
        {
            Name = "ToDelete",
            AccountId = account.Id,
            Period = "Monthly",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Amount = 1000
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/budgets", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<BudgetResponse>>())!.Data;

        var response = await _client.DeleteAsync($"/api/v1/budgets/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/v1/budgets", new CreateBudgetRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
