using System.Net;
using System.Net.Http.Json;
using DashboardAnalytics.IntegrationTests.Fixtures;
using DashboardAnalytics.Models.DTOs;

namespace DashboardAnalytics.IntegrationTests.Controllers;

[Collection("Integration")]
[Trait("Category", "Integration")]
public class KPIsControllerTests : IAsyncLifetime
{
    private readonly DashboardIntegrationFixture _fixture;
    private readonly HttpClient _client;

    public KPIsControllerTests(DashboardIntegrationFixture fixture)
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
    public async Task GetAll_ReturnsEmptyList_WhenNoKPIs()
    {
        var response = await _client.GetAsync("/api/v1/kpis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_ReturnsCreatedKPI()
    {
        var request = new CreateKPIRequest("Revenue Target", "Monthly revenue target", 50000);

        var response = await _client.PostAsJsonAsync("/api/v1/kpis", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<KPIResponse>>();
        result!.Success.Should().BeTrue();
        result.Data.Name.Should().Be("Revenue Target");
        result.Data.TargetValue.Should().Be(50000);
    }

    [Fact]
    public async Task GetById_ReturnsKPI_AfterCreate()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/kpis",
            new CreateKPIRequest("Lookup KPI", "For lookup test", 100));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<KPIResponse>>())!.Data;

        var response = await _client.GetAsync($"/api/v1/kpis/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<KPIResponse>>();
        result!.Data.Id.Should().Be(created.Id);
        result.Data.Name.Should().Be("Lookup KPI");
    }

    [Fact]
    public async Task Update_ModifiesKPI()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/kpis",
            new CreateKPIRequest("Updateable KPI", "Will be updated", 200));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<KPIResponse>>())!.Data;

        var updateRequest = new UpdateKPIRequest(150, 300);
        var response = await _client.PutAsJsonAsync($"/api/v1/kpis/{created.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/kpis/{created.Id}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<KPIResponse>>();
        result!.Data.CurrentValue.Should().Be(150);
        result.Data.TargetValue.Should().Be(300);
    }

    [Fact]
    public async Task Delete_RemovesKPI()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/v1/kpis",
            new CreateKPIRequest("ToDelete", "Will be deleted", 100));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<KPIResponse>>())!.Data;

        var response = await _client.DeleteAsync($"/api/v1/kpis/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/v1/kpis/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WithoutToken()
    {
        using var unauthClient = _fixture.Factory.CreateClient();

        var response = await unauthClient.PostAsJsonAsync("/api/v1/kpis",
            new CreateKPIRequest("Test", "Test", 100));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
