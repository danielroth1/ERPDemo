using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using FinancialManagement.IntegrationTests.Fixtures;
using FinancialManagement.Models.DTOs;

namespace FinancialManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class AccountsControllerTests : IAsyncLifetime
{
    private readonly FinancialIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public AccountsControllerTests(FinancialIntegrationFixture fixture)
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

    private CreateAccountRequest CreateAccountRequest(string name = "Test Account") => new()
    {
        Name = name,
        Type = "Asset",
        Category = "CurrentAssets",
        Currency = "USD",
        Description = "Integration test account"
    };

    [Fact]
    public async Task Create_ReturnsCreatedAccount()
    {
        var request = CreateAccountRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/accounts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.Name.Should().Be("Test Account");
        result.Data.Type.Should().Be("Asset");
        result.Data.Category.Should().Be("CurrentAssets");
        result.Data.Currency.Should().Be("USD");
        result.Data.Balance.Should().Be(0);
        result.Data.IsActive.Should().BeTrue();
        result.Data.AccountNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_ReturnsAccount_AfterCreate()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", CreateAccountRequest());
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/accounts/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>();
        result!.Data.Id.Should().Be(created.Id);
        result.Data.Name.Should().Be("Test Account");
    }

    [Fact]
    public async Task GetByNumber_ReturnsAccount()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", CreateAccountRequest("Numbered"));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/accounts/number/{created.AccountNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>();
        result!.Data.AccountNumber.Should().Be(created.AccountNumber);
    }

    [Fact]
    public async Task GetAll_ReturnsAccounts()
    {
        await _client.PostAsJsonAsync("/api/v1/accounts", CreateAccountRequest("Account A"));
        await _client.PostAsJsonAsync("/api/v1/accounts", CreateAccountRequest("Account B"));

        var response = await _client.GetAsync("/api/v1/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AccountResponse>>>();
        result!.Data.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetByType_ReturnsFilteredAccounts()
    {
        await _client.PostAsJsonAsync("/api/v1/accounts", CreateAccountRequest("Asset Account"));
        var liabilityRequest = CreateAccountRequest("Liability Account");
        liabilityRequest.Type = "Liability";
        liabilityRequest.Category = "CurrentLiabilities";
        await _client.PostAsJsonAsync("/api/v1/accounts", liabilityRequest);

        var response = await _client.GetAsync("/api/v1/accounts/type/Asset");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AccountResponse>>>();
        result!.Data.Should().OnlyContain(a => a.Type == "Asset");
    }

    [Fact]
    public async Task Update_ModifiesAccount()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", CreateAccountRequest());
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>())!.Data;

        var updateRequest = new UpdateAccountRequest { Name = "Updated Account", Description = "Updated" };
        var response = await _client.PutAsJsonAsync($"/api/v1/accounts/{created.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/accounts/{created.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>();
        result!.Data.Name.Should().Be("Updated Account");
    }

    [Fact]
    public async Task Delete_SoftDeletesAccount()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/accounts", CreateAccountRequest("ToDelete"));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AccountResponse>>())!.Data;

        var response = await _client.DeleteAsync($"/api/v1/accounts/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/v1/accounts", CreateAccountRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DbReset_EnsuresIsolation_BetweenTests()
    {
        var response = await _client.GetAsync("/api/v1/accounts");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<AccountResponse>>>();
        result!.Data.Should().BeEmpty();
    }
}
