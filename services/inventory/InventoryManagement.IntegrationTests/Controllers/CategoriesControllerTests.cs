using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using InventoryManagement.IntegrationTests.Fixtures;
using InventoryManagement.Models.DTOs;

namespace InventoryManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class CategoriesControllerTests : IAsyncLifetime
{
    private readonly InventoryIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public CategoriesControllerTests(InventoryIntegrationFixture fixture)
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

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoCategories()
    {
        var response = await _client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryResponse>>>();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ReturnsCreatedCategory()
    {
        var request = new CategoryRequest
        {
            Name = "Electronics",
            Description = "Electronic products"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.Name.Should().Be("Electronics");
        result.Data.Description.Should().Be("Electronic products");
        result.Data.IsActive.Should().BeTrue();
        result.Data.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetById_ReturnsCategory_AfterCreate()
    {
        // Arrange — create a category
        var createRequest = new CategoryRequest { Name = "Books", Description = "Book products" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/categories", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data;

        // Act
        var response = await _client.GetAsync($"/api/v1/categories/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>();
        result!.Data.Id.Should().Be(created.Id);
        result.Data.Name.Should().Be("Books");
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForNonExistentId()
    {
        var response = await _client.GetAsync("/api/v1/categories/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ModifiesCategory()
    {
        // Arrange
        var createRequest = new CategoryRequest { Name = "Original", Description = "Original desc" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/categories", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data;

        var updateRequest = new CategoryRequest { Name = "Updated", Description = "Updated desc" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/categories/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/categories/{created.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>();
        result!.Data.Name.Should().Be("Updated");
        result!.Data.Description.Should().Be("Updated desc");
    }

    [Fact]
    public async Task Delete_RemovesCategory()
    {
        // Arrange
        var createRequest = new CategoryRequest { Name = "ToDelete", Description = "Will be deleted" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/categories", createRequest);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryResponse>>())!.Data;

        // Act
        var response = await _client.DeleteAsync($"/api/v1/categories/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/categories/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();
        var request = new CategoryRequest { Name = "Test", Description = "Test" };

        var response = await unauthClient.PostAsJsonAsync("/api/v1/categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DbReset_EnsuresIsolation_BetweenTests()
    {
        // This test verifies that DB reset works — should always start with 0 categories
        var response = await _client.GetAsync("/api/v1/categories");
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<CategoryResponse>>>();
        result!.Data.Should().BeEmpty();
    }
}
