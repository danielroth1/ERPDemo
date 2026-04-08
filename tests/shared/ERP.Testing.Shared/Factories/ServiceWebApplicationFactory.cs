using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using WireMock.Server;

namespace ERP.Testing.Shared.Factories;

/// <summary>
/// Custom WebApplicationFactory that replaces the real PostgreSQL connection
/// with a Testcontainers-provided one, optionally stubs external HTTP services
/// via WireMock, and disables infrastructure not needed in tests.
/// </summary>
/// <typeparam name="TProgram">The service's Program entry point type.</typeparam>
/// <typeparam name="TDbContext">The service's EF Core DbContext type.</typeparam>
public class ServiceWebApplicationFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>
    where TProgram : class
    where TDbContext : DbContext
{
    private readonly string _postgresConnectionString;
    private readonly Action<IServiceCollection>? _configureServices;

    public WireMockServer? WireMockServer { get; private set; }

    public ServiceWebApplicationFactory(
        string postgresConnectionString,
        bool enableWireMock = false,
        Action<IServiceCollection>? configureServices = null)
    {
        _postgresConnectionString = postgresConnectionString;
        _configureServices = configureServices;

        if (enableWireMock)
        {
            WireMockServer = WireMockServer.Start();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration and replace with test Postgres
            services.RemoveAll<DbContextOptions<TDbContext>>();
            services.RemoveAll<TDbContext>();

            // Remove any DbContextFactory registrations too
            services.RemoveAll<IDbContextFactory<TDbContext>>();

            services.AddDbContext<TDbContext>(options =>
                options.UseNpgsql(_postgresConnectionString)
                    .UseSnakeCaseNamingConvention());

            // Also register DbContextFactory for services that use it (e.g., GraphQL DataLoaders)
            services.AddDbContextFactory<TDbContext>(options =>
                options.UseNpgsql(_postgresConnectionString)
                    .UseSnakeCaseNamingConvention());

            // Ensure schema is created on first use
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
            db.Database.EnsureCreated();

            _configureServices?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WireMockServer?.Stop();
            WireMockServer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
