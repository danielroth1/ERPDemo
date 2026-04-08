using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using InventoryManagement.IntegrationTests.Fixtures;
using InventoryManagement.Models.DTOs;

namespace InventoryManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class ProductsControllerTests : IAsyncLifetime
{
    private readonly InventoryIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public ProductsControllerTests(InventoryIntegrationFixture fixture)
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

    private async Task<CategoryResponse> CreateCategoryAsync(string name = "Test Category")
    {
        var request = new CategoryRequest { Name = name, Description = $"{name} description" };
        var response = await _client.PostAsJsonAsync("/api/v1/categories", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data;
    }

    private ProductRequest CreateProductRequest(string categoryId) => new()
    {
        Sku = $"SKU-{Guid.NewGuid():N}".Substring(0, 20),
        Name = "Test Product",
        Description = "A test product",
        CategoryId = categoryId,
        Price = 29.99m,
        Cost = 15.00m,
        StockQuantity = 100,
        MinStockLevel = 10,
        MaxStockLevel = 1000,
        Unit = "pcs"
    };

    [Fact]
    public async Task GetAll_ReturnsEmptyPaginatedResponse_WhenNoProducts()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResponse<ProductResponse>>>();
        result!.Success.Should().BeTrue();
        result.Data.Items.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_ReturnsCreatedProduct_WithRealPostgresDefaults()
    {
        var category = await CreateCategoryAsync();
        var request = CreateProductRequest(category.Id);

        var response = await _client.PostAsJsonAsync("/api/v1/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.Name.Should().Be("Test Product");
        result.Data.CategoryId.Should().Be(category.Id);
        result.Data.Price.Should().Be(29.99m);
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeNullOrEmpty();
        // Postgres-generated default — wouldn't work with InMemory
        result.Data.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task GetById_ReturnsProduct_AfterCreate()
    {
        var category = await CreateCategoryAsync();
        var request = CreateProductRequest(category.Id);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/products", request);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/products/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>();
        result!.Data.Id.Should().Be(created.Id);
        result.Data.Sku.Should().Be(request.Sku);
    }

    [Fact]
    public async Task GetBySku_ReturnsProduct()
    {
        var category = await CreateCategoryAsync();
        var request = CreateProductRequest(category.Id);
        await _client.PostAsJsonAsync("/api/v1/products", request);

        var response = await _client.GetAsync($"/api/v1/products/sku/{request.Sku}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductResponse>>();
        result!.Data.Sku.Should().Be(request.Sku);
    }

    [Fact]
    public async Task Search_FindsProducts_ByName()
    {
        var category = await CreateCategoryAsync();
        var request = CreateProductRequest(category.Id);
        request.Name = "Unique Searchable Widget";
        await _client.PostAsJsonAsync("/api/v1/products", request);

        var response = await _client.GetAsync("/api/v1/products/search?q=Searchable");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductResponse>>>();
        result!.Data.Should().ContainSingle(p => p.Name == "Unique Searchable Widget");
    }

    [Fact]
    public async Task GetByCategory_ReturnsOnlyMatchingProducts()
    {
        var cat1 = await CreateCategoryAsync("Category A");
        var cat2 = await CreateCategoryAsync("Category B");

        var product1 = CreateProductRequest(cat1.Id);
        product1.Name = "Product A";
        var product2 = CreateProductRequest(cat2.Id);
        product2.Name = "Product B";

        await _client.PostAsJsonAsync("/api/v1/products", product1);
        await _client.PostAsJsonAsync("/api/v1/products", product2);

        var response = await _client.GetAsync($"/api/v1/products/category/{cat1.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductResponse>>>();
        result!.Data.Should().ContainSingle(p => p.Name == "Product A");
    }

    [Fact]
    public async Task Pagination_WorksCorrectly()
    {
        var category = await CreateCategoryAsync();
        // Create 5 products
        for (var i = 0; i < 5; i++)
        {
            var req = CreateProductRequest(category.Id);
            req.Name = $"Product {i}";
            await _client.PostAsJsonAsync("/api/v1/products", req);
        }

        var response = await _client.GetAsync("/api/v1/products?page=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedResponse<ProductResponse>>>();
        result!.Data.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(5);
        result.Data.TotalPages.Should().Be(3);
        result.Data.Page.Should().Be(1);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();
        var request = new ProductRequest { Name = "Test" };

        var response = await unauthClient.PostAsJsonAsync("/api/v1/products", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
