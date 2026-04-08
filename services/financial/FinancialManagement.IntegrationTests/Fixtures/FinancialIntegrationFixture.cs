using DomainEvents = ERP.Contracts.Events.Domain;
using ERP.Testing.Shared.Auth;
using ERP.Testing.Shared.Fixtures;
using FinancialManagement.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Text;

namespace FinancialManagement.IntegrationTests.Fixtures;

public class FinancialIntegrationFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public DatabaseResetter DbResetter { get; private set; } = null!;

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

                Environment.SetEnvironmentVariable("ConnectionStrings__erp-financial", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("ConnectionStrings__kafka", "localhost:9092");
                Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", "amqp://guest:guest@localhost:5672");
                Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtTokenHelper.TestSecret);
                Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtTokenHelper.TestIssuer);
                Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtTokenHelper.TestAudience);

                builder.ConfigureServices(services =>
                {
                    // Remove the NpgsqlDataSource registered by Program.cs (EnableDynamicJson)
                    var dataSourceDescriptors = services
                        .Where(d => d.ServiceType.FullName?.Contains("NpgsqlDataSource") == true ||
                                    d.ServiceType.FullName?.Contains("Npgsql") == true)
                        .ToList();
                    foreach (var d in dataSourceDescriptors) services.Remove(d);

                    // Replace DbContext with Testcontainers Postgres
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<DbContext>();
                    services.RemoveAll<AppDbContext>();

                    var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(Postgres.ConnectionString);
                    dataSourceBuilder.EnableDynamicJson();
                    var dataSource = dataSourceBuilder.Build();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(dataSource)
                            .UseSnakeCaseNamingConvention());

                    // Remove all MassTransit services (RabbitMQ consumers, Kafka rider, outbox)
                    var toRemove = services
                        .Where(d =>
                            (d.ServiceType.FullName?.Contains("MassTransit") == true) ||
                            (d.ImplementationType?.FullName?.Contains("MassTransit") == true) ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(ITopicProducer<>)))
                        .ToList();
                    foreach (var d in toRemove) services.Remove(d);

                    // Register mock topic producers
                    services.AddSingleton<ITopicProducer<DomainEvents.TransactionCreated>>(_ => Mock.Of<ITopicProducer<DomainEvents.TransactionCreated>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.BudgetExceeded>>(_ => Mock.Of<ITopicProducer<DomainEvents.BudgetExceeded>>());

                    services.AddMassTransitTestHarness();

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

                    // Schema is created by Program.cs Database.Migrate()
                    // Just build a service provider to trigger startup
                });
            });

        // Force the host to start so Database.Migrate() runs
        _ = Factory.Server;

        // Ignore MassTransit outbox tables when resetting
        DbResetter = new DatabaseResetter(
            Postgres.ConnectionString,
            "__EFMigrationsHistory", "inbox_state", "outbox_message", "outbox_state", "processed_messages");
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();

        Environment.SetEnvironmentVariable("ConnectionStrings__erp-financial", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", null);
        Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);

        await Postgres.DisposeAsync();
    }
}
