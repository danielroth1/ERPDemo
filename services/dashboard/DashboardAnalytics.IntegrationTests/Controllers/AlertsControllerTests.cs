using System.Net;
using System.Net.Http.Json;
using DashboardAnalytics.IntegrationTests.Fixtures;
using DashboardAnalytics.Models.DTOs;

namespace DashboardAnalytics.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class AlertsControllerTests : IAsyncLifetime
{
    private readonly DashboardIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public AlertsControllerTests(DashboardIntegrationFixture fixture)
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
    public async Task GetAll_ReturnsEmptyList_WhenNoAlerts()
    {
        var response = await _client.GetAsync("/api/v1/alerts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnread_ReturnsEmptyList_WhenNoAlerts()
    {
        var response = await _client.GetAsync("/api/v1/alerts/unread");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/alerts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
