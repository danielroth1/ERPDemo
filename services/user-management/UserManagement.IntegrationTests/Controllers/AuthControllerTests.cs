using System.Net;
using System.Net.Http.Json;
using UserManagement.IntegrationTests.Fixtures;
using UserManagement.Models.DTOs;

namespace UserManagement.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class AuthControllerTests : IAsyncLifetime
{
    private readonly UserManagementIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public AuthControllerTests(UserManagementIntegrationFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
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
    public async Task Register_ReturnsSuccess_WithValidData()
    {
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.User.Email.Should().Be("test@example.com");
        result.Data.User.FirstName.Should().Be("Test");
        result.Data.User.LastName.Should().Be("User");
        result.Data.User.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_ForDuplicateEmail()
    {
        var request = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ReturnsTokens_WithValidCredentials()
    {
        // Register first
        var registerRequest = new RegisterRequest
        {
            Email = "login@example.com",
            Password = "Password123!",
            FirstName = "Login",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "login@example.com",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        result.Data.User.Email.Should().Be("login@example.com");
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WithWrongPassword()
    {
        var registerRequest = new RegisterRequest
        {
            Email = "wrongpw@example.com",
            Password = "Password123!",
            FirstName = "Wrong",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = "wrongpw@example.com",
            Password = "WrongPassword!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMe_ReturnsCurrentUser_WithValidToken()
    {
        // Register and login to get a real token from the service
        var registerRequest = new RegisterRequest
        {
            Email = "me@example.com",
            Password = "Password123!",
            FirstName = "Me",
            LastName = "User"
        };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest { Email = "me@example.com", Password = "Password123!" });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Data.AccessToken);

        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserResponse>>();
        result!.Data.Email.Should().Be("me@example.com");
    }

    [Fact]
    public async Task GetMe_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DbReset_EnsuresIsolation_BetweenTests()
    {
        // After DB reset, registering with any email should succeed
        var request = new RegisterRequest
        {
            Email = "isolated@example.com",
            Password = "Password123!",
            FirstName = "Isolated",
            LastName = "Test"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
