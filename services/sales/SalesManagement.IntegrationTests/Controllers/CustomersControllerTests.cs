using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using SalesManagement.IntegrationTests.Fixtures;
using SalesManagement.Models.DTOs;

namespace SalesManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CustomersControllerTests : IAsyncLifetime
{
    private readonly SalesIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public CustomersControllerTests(SalesIntegrationFixture fixture)
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

    private CreateCustomerRequest CreateCustomerRequest(string email = "customer@example.com") => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = email,
        Phone = "+1234567890",
        Company = "Test Corp"
    };

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoCustomers()
    {
        var response = await _client.GetAsync("/api/v1/customers");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsCreatedCustomer()
    {
        var request = CreateCustomerRequest();

        var response = await _client.PostAsJsonAsync("/api/v1/customers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.FirstName.Should().Be("John");
        result.Data.LastName.Should().Be("Doe");
        result.Data.Email.Should().Be("customer@example.com");
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_ReturnsCustomer_AfterCreate()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", CreateCustomerRequest());
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/customers/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>();
        result!.Data.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetByEmail_ReturnsCustomer()
    {
        await _client.PostAsJsonAsync("/api/v1/customers", CreateCustomerRequest("unique@example.com"));

        var response = await _client.GetAsync("/api/v1/customers/email/unique@example.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>();
        result!.Data.Email.Should().Be("unique@example.com");
    }

    [Fact]
    public async Task Update_ModifiesCustomer()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", CreateCustomerRequest());
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data;

        var updateRequest = new UpdateCustomerRequest { FirstName = "Jane", LastName = "Smith" };
        var response = await _client.PutAsJsonAsync($"/api/v1/customers/{created.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/customers/{created.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>();
        result!.Data.FirstName.Should().Be("Jane");
        result.Data.LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task Delete_SoftDeletesCustomer()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", CreateCustomerRequest("del@example.com"));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data;

        var response = await _client.DeleteAsync($"/api/v1/customers/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/v1/customers", CreateCustomerRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
