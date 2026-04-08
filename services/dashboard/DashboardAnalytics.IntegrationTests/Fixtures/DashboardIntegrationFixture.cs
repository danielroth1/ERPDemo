using DashboardAnalytics.Infrastructure;
using ERP.Testing.Shared.Auth;
using ERP.Testing.Shared.Fixtures;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DashboardAnalytics.IntegrationTests.Fixtures;

public class DashboardIntegrationFixture : IAsyncLifetime
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

                Environment.SetEnvironmentVariable("ConnectionStrings__erp-dashboard", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("ConnectionStrings__kafka", "localhost:9092");
                Environment.SetEnvironmentVariable("ConnectionStrings__redis", "localhost:6379");
                Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtTokenHelper.TestSecret);
                Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtTokenHelper.TestIssuer);
                Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtTokenHelper.TestAudience);

                builder.ConfigureServices(services =>
                {
                    // Replace DbContext
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<DbContext>();
                    services.RemoveAll<AppDbContext>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(Postgres.ConnectionString)
                            .UseSnakeCaseNamingConvention());

                    // Replace Redis distributed cache with in-memory cache
                    services.RemoveAll<IDistributedCache>();
                    services.AddSingleton<IDistributedCache>(
                        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

                    // Remove all MassTransit services (Kafka consumers)
                    var toRemove = services
                        .Where(d =>
                            (d.ServiceType.FullName?.Contains("MassTransit") == true) ||
                            (d.ImplementationType?.FullName?.Contains("MassTransit") == true) ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(ITopicProducer<>)))
                        .ToList();
                    foreach (var d in toRemove) services.Remove(d);

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

        Environment.SetEnvironmentVariable("ConnectionStrings__erp-dashboard", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", null);
        Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);

        await Postgres.DisposeAsync();
    }
}
