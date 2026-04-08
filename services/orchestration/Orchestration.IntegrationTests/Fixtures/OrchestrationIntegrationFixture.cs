using DomainEvents = ERP.Contracts.Events;
using ERP.Testing.Shared.Auth;
using ERP.Testing.Shared.Fixtures;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Orchestration.Infrastructure;
using Orchestration.Services;
using System.Text;

namespace Orchestration.IntegrationTests.Fixtures;

public class OrchestrationIntegrationFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public DatabaseResetter DbResetter { get; private set; } = null!;

    /// <summary>
    /// The PurchaseTracker singleton — tests can call TryComplete/TryFail on it
    /// to simulate saga completion while an HTTP request is in-flight.
    /// </summary>
    public PurchaseTracker PurchaseTracker { get; } = new();
    public ReturnTracker ReturnTracker { get; } = new();

    public HttpClient CreateAuthenticatedClient(string[] roles = null!)
    {
        roles ??= ["Admin"];
        var client = Factory.CreateClient();
        client.SetBearerToken(TestJwtTokenHelper.GenerateToken(roles: roles));
        return client;
    }

    public async Task InitializeAsync()
    {
        await Postgres.InitializeAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                Environment.SetEnvironmentVariable("ConnectionStrings__erp-orchestration", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("ConnectionStrings__kafka", "localhost:9092");
                Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", "amqp://guest:guest@localhost:5672");
                Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtTokenHelper.TestSecret);
                Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtTokenHelper.TestIssuer);
                Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtTokenHelper.TestAudience);

                builder.ConfigureServices(services =>
                {
                    // Replace DbContext
                    services.RemoveAll<DbContextOptions<OrchestrationDbContext>>();
                    services.RemoveAll<OrchestrationDbContext>();

                    services.AddDbContext<OrchestrationDbContext>(options =>
                        options.UseNpgsql(Postgres.ConnectionString)
                            .UseSnakeCaseNamingConvention());

                    // Remove all MassTransit services (RabbitMQ, Kafka, sagas, outbox)
                    var toRemove = services
                        .Where(d =>
                            (d.ServiceType.FullName?.Contains("MassTransit") == true) ||
                            (d.ImplementationType?.FullName?.Contains("MassTransit") == true) ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(ITopicProducer<>)))
                        .ToList();
                    foreach (var d in toRemove) services.Remove(d);

                    // Register mock Kafka producers
                    services.AddSingleton<ITopicProducer<DomainEvents.PurchaseCompleted>>(_ => Mock.Of<ITopicProducer<DomainEvents.PurchaseCompleted>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.PurchaseFailed>>(_ => Mock.Of<ITopicProducer<DomainEvents.PurchaseFailed>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.ReturnCompleted>>(_ => Mock.Of<ITopicProducer<DomainEvents.ReturnCompleted>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.ReturnFailed>>(_ => Mock.Of<ITopicProducer<DomainEvents.ReturnFailed>>());

                    services.AddMassTransitTestHarness();

                    // Replace trackers with shared instances the test can access
                    services.RemoveAll<PurchaseTracker>();
                    services.RemoveAll<ReturnTracker>();
                    services.AddSingleton(PurchaseTracker);
                    services.AddSingleton(ReturnTracker);

                    // Remove SagaTimeoutService background service
                    var sagaTimeout = services
                        .Where(d => d.ImplementationType == typeof(SagaTimeoutService))
                        .ToList();
                    foreach (var d in sagaTimeout) services.Remove(d);

                    // Override JWT auth
                    services.Configure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        options =>
                        {
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                IssuerSigningKey = new SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(TestJwtTokenHelper.TestSecret)),
                                ValidateIssuer = true,
                                ValidIssuer = TestJwtTokenHelper.TestIssuer,
                                ValidateAudience = true,
                                ValidAudience = TestJwtTokenHelper.TestAudience,
                                ValidateLifetime = true,
                                ClockSkew = TimeSpan.Zero
                            };
                        });

                });
            });

        // Trigger host startup — Program.cs calls Database.Migrate() which creates the schema.
        // Do NOT use EnsureCreated() as it conflicts with Migrate().
        _ = Factory.Server;

        DbResetter = new DatabaseResetter(
            Postgres.ConnectionString,
            "__EFMigrationsHistory", "inbox_state", "outbox_message", "outbox_state");
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();

        Environment.SetEnvironmentVariable("ConnectionStrings__erp-orchestration", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", null);
        Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);

        await Postgres.DisposeAsync();
    }
}
