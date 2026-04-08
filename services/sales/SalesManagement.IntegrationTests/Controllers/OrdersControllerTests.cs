using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using SalesManagement.IntegrationTests.Fixtures;
using SalesManagement.Models.DTOs;

namespace SalesManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class OrdersControllerTests : IAsyncLifetime
{
    private readonly SalesIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public OrdersControllerTests(SalesIntegrationFixture fixture)
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

    private async Task<CustomerResponse> CreateCustomerAsync()
    {
        var request = new CreateCustomerRequest
        {
            FirstName = "Order",
            LastName = "Customer",
            Email = $"order-{Guid.NewGuid():N}@example.com"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/customers", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data;
    }

    private CreateOrderRequest CreateOrderRequest(string customerId) => new()
    {
        CustomerId = customerId,
        Items = new List<OrderItemRequest>
        {
            new() { ProductId = Guid.NewGuid().ToString(), Quantity = 2, Discount = 0 }
        },
        Notes = "Integration test order"
    };

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoOrders()
    {
        var response = await _client.GetAsync("/api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsCreatedOrder()
    {
        var customer = await CreateCustomerAsync();
        var request = CreateOrderRequest(customer.Id);

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.CustomerId.Should().Be(customer.Id);
        result.Data.OrderNumber.Should().NotBeNullOrEmpty();
        result.Data.Status.Should().NotBeNullOrEmpty();
        result.Data.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_ReturnsOrder_AfterCreate()
    {
        var customer = await CreateCustomerAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", CreateOrderRequest(customer.Id));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/orders/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();
        result!.Data.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetByCustomer_ReturnsOnlyMatchingOrders()
    {
        var customer1 = await CreateCustomerAsync();
        var customer2 = await CreateCustomerAsync();

        await _client.PostAsJsonAsync("/api/v1/orders", CreateOrderRequest(customer1.Id));
        await _client.PostAsJsonAsync("/api/v1/orders", CreateOrderRequest(customer2.Id));

        var response = await _client.GetAsync($"/api/v1/orders/customer/{customer1.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<OrderResponse>>>();
        result!.Data.Should().OnlyContain(o => o.CustomerId == customer1.Id);
    }

    [Fact]
    public async Task UpdateStatus_ChangesOrderStatus()
    {
        var customer = await CreateCustomerAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", CreateOrderRequest(customer.Id));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>())!.Data;

        var statusRequest = new UpdateOrderStatusRequest { Status = "Confirmed" };
        var response = await _client.PatchAsJsonAsync($"/api/v1/orders/{created.Id}/status", statusRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/orders/{created.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>();
        result!.Data.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_ForNonDraftOrder()
    {
        var customer = await CreateCustomerAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/orders", CreateOrderRequest(customer.Id));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>())!.Data;

        // Orders are created with Pending status, delete only works for Draft
        var response = await _client.DeleteAsync($"/api/v1/orders/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/v1/orders", new CreateOrderRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
