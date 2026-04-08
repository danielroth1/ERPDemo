using DomainEvents = ERP.Contracts.Events.Domain;
using ERP.Testing.Shared.Auth;
using ERP.Testing.Shared.Fixtures;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Npgsql;
using SalesManagement.Infrastructure;
using System.Text;

namespace SalesManagement.IntegrationTests.Fixtures;

public class SalesIntegrationFixture : IAsyncLifetime
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

                Environment.SetEnvironmentVariable("ConnectionStrings__erp-sales", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("ConnectionStrings__kafka", "localhost:9092");
                Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtTokenHelper.TestSecret);
                Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtTokenHelper.TestIssuer);
                Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtTokenHelper.TestAudience);

                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with NpgsqlDataSource that has EnableDynamicJson
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<DbContext>();
                    services.RemoveAll<AppDbContext>();

                    // Remove any existing NpgsqlDataSource registrations
                    var npgsqlDescriptors = services
                        .Where(d => d.ServiceType.FullName?.Contains("NpgsqlDataSource") == true ||
                                    d.ImplementationType?.FullName?.Contains("NpgsqlDataSource") == true)
                        .ToList();
                    foreach (var d in npgsqlDescriptors) services.Remove(d);

                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(Postgres.ConnectionString);
                    dataSourceBuilder.EnableDynamicJson();
                    var dataSource = dataSourceBuilder.Build();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(dataSource)
                            .UseSnakeCaseNamingConvention());

                    // Remove all MassTransit services
                    var toRemove = services
                        .Where(d =>
                            (d.ServiceType.FullName?.Contains("MassTransit") == true) ||
                            (d.ImplementationType?.FullName?.Contains("MassTransit") == true) ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(ITopicProducer<>)))
                        .ToList();
                    foreach (var d in toRemove) services.Remove(d);

                    // Register mock topic producers
                    services.AddSingleton<ITopicProducer<DomainEvents.OrderCreated>>(_ => Mock.Of<ITopicProducer<DomainEvents.OrderCreated>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.OrderStatusChanged>>(_ => Mock.Of<ITopicProducer<DomainEvents.OrderStatusChanged>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.InvoiceCreated>>(_ => Mock.Of<ITopicProducer<DomainEvents.InvoiceCreated>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.InvoicePaid>>(_ => Mock.Of<ITopicProducer<DomainEvents.InvoicePaid>>());

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

                    // Ensure DB schema
                    using var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();
                });
            });

        DbResetter = new DatabaseResetter(Postgres.ConnectionString, "__EFMigrationsHistory");
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();

        Environment.SetEnvironmentVariable("ConnectionStrings__erp-sales", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
        Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);

        await Postgres.DisposeAsync();
    }
}
