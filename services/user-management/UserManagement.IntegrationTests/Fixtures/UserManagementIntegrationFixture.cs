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
using System.Text;
using UserManagement.Infrastructure;

namespace UserManagement.IntegrationTests.Fixtures;

public class UserManagementIntegrationFixture : IAsyncLifetime
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

                Environment.SetEnvironmentVariable("ConnectionStrings__erp-users", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("ConnectionStrings__kafka", "localhost:9092");
                Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtTokenHelper.TestSecret);
                Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtTokenHelper.TestIssuer);
                Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtTokenHelper.TestAudience);
                Environment.SetEnvironmentVariable("Smtp__Host", "localhost");
                Environment.SetEnvironmentVariable("Smtp__Port", "25");
                Environment.SetEnvironmentVariable("Smtp__Username", "test");
                Environment.SetEnvironmentVariable("Smtp__Password", "test");
                Environment.SetEnvironmentVariable("Smtp__FromEmail", "test@test.com");
                Environment.SetEnvironmentVariable("Smtp__FromName", "Test");

                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with Testcontainers Postgres
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<DbContext>();
                    services.RemoveAll<AppDbContext>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(Postgres.ConnectionString)
                            .UseSnakeCaseNamingConvention());

                    // Remove all MassTransit services to prevent Kafka connection
                    var toRemove = services
                        .Where(d =>
                            (d.ServiceType.FullName?.Contains("MassTransit") == true) ||
                            (d.ImplementationType?.FullName?.Contains("MassTransit") == true) ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(ITopicProducer<>)))
                        .ToList();
                    foreach (var d in toRemove) services.Remove(d);

                    // Register mock topic producers
                    services.AddSingleton<ITopicProducer<DomainEvents.UserCreated>>(_ => Mock.Of<ITopicProducer<DomainEvents.UserCreated>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.UserUpdated>>(_ => Mock.Of<ITopicProducer<DomainEvents.UserUpdated>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.UserDeleted>>(_ => Mock.Of<ITopicProducer<DomainEvents.UserDeleted>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.UserDeactivated>>(_ => Mock.Of<ITopicProducer<DomainEvents.UserDeactivated>>());

                    services.AddMassTransitTestHarness();

                    // Override JWT auth to use test keys
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

                    // Ensure DB schema is created
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

        Environment.SetEnvironmentVariable("ConnectionStrings__erp-users", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
        Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Smtp__Host", null);
        Environment.SetEnvironmentVariable("Smtp__Port", null);
        Environment.SetEnvironmentVariable("Smtp__Username", null);
        Environment.SetEnvironmentVariable("Smtp__Password", null);
        Environment.SetEnvironmentVariable("Smtp__FromEmail", null);
        Environment.SetEnvironmentVariable("Smtp__FromName", null);

        await Postgres.DisposeAsync();
    }
}
