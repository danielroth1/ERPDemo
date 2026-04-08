using System.Net;
using System.Net.Http.Json;
using ERP.Testing.Shared.Auth;
using UserManagement.IntegrationTests.Fixtures;
using UserManagement.Models.DTOs;

namespace UserManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class UsersControllerTests : IAsyncLifetime
{
    private readonly UserManagementIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public UsersControllerTests(UserManagementIntegrationFixture fixture)
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

    private async Task<UserResponse> RegisterUserAsync(string email = "user@example.com")
    {
        using var unauthClient = _fixture.Factory.CreateClient();
        var request = new RegisterRequest
        {
            Email = email,
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };
        var response = await unauthClient.PostAsJsonAsync("/api/v1/auth/register", request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        return result!.Data.User;
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoUsers()
    {
        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_ReturnsUser_AfterRegistration()
    {
        var user = await RegisterUserAsync();

        var response = await _client.GetAsync($"/api/v1/users/{user.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        result!.Data.Id.Should().Be(user.Id);
        result.Data.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_ForNonExistentId()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_RemovesUser()
    {
        var user = await RegisterUserAsync("delete@example.com");

        var response = await _client.DeleteAsync($"/api/v1/users/{user.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/users/{user.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_DisablesUser()
    {
        var user = await RegisterUserAsync("deactivate@example.com");

        var response = await _client.PostAsync($"/api/v1/users/{user.Id}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/users/{user.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        result!.Data.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
