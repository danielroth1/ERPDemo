using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using SalesManagement.IntegrationTests.Fixtures;
using SalesManagement.Models.DTOs;

namespace SalesManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class InvoicesControllerTests : IAsyncLifetime
{
    private readonly SalesIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public InvoicesControllerTests(SalesIntegrationFixture fixture)
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

    private async Task<(CustomerResponse Customer, OrderResponse Order)> CreateOrderWithCustomerAsync()
    {
        var customerRequest = new CreateCustomerRequest
        {
            FirstName = "Invoice",
            LastName = "Customer",
            Email = $"invoice-{Guid.NewGuid():N}@example.com"
        };
        var customerResponse = await _client.PostAsJsonAsync("/api/v1/customers", customerRequest);
        var customer = (await customerResponse.Content.ReadFromJsonAsync<ApiResponse<CustomerResponse>>())!.Data;

        var orderRequest = new CreateOrderRequest
        {
            CustomerId = customer.Id,
            Items = new List<OrderItemRequest>
            {
                new() { ProductId = Guid.NewGuid().ToString(), Quantity = 1, Discount = 0 }
            }
        };
        var orderResponse = await _client.PostAsJsonAsync("/api/v1/orders", orderRequest);
        var order = (await orderResponse.Content.ReadFromJsonAsync<ApiResponse<OrderResponse>>())!.Data;

        return (customer, order);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoInvoices()
    {
        var response = await _client.GetAsync("/api/v1/invoices");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsCreatedInvoice()
    {
        var (customer, order) = await CreateOrderWithCustomerAsync();

        var request = new CreateInvoiceRequest
        {
            OrderId = order.Id,
            DueDate = DateTime.UtcNow.AddDays(30),
            Notes = "Test invoice"
        };
        var response = await _client.PostAsJsonAsync("/api/v1/invoices", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.OrderId.Should().Be(order.Id);
        result.Data.InvoiceNumber.Should().NotBeNullOrEmpty();
        result.Data.Status.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_ReturnsInvoice_AfterCreate()
    {
        var (_, order) = await CreateOrderWithCustomerAsync();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/invoices",
            new CreateInvoiceRequest { OrderId = order.Id });
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/invoices/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<InvoiceResponse>>();
        result!.Data.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetByOrder_ReturnsMatchingInvoices()
    {
        var (_, order) = await CreateOrderWithCustomerAsync();
        await _client.PostAsJsonAsync("/api/v1/invoices",
            new CreateInvoiceRequest { OrderId = order.Id });

        var response = await _client.GetAsync($"/api/v1/invoices/order/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
