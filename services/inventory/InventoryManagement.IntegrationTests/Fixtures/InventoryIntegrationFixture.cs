using DomainEvents = ERP.Contracts.Events.Domain;
using ERP.Testing.Shared.Auth;
using ERP.Testing.Shared.Fixtures;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models.DTOs;
using InventoryManagement.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Text;
using WireMock.Server;

namespace InventoryManagement.IntegrationTests.Fixtures;

/// <summary>
/// Fixture that starts a real PostgreSQL container and WireMock server,
/// then boots the Inventory service with those dependencies.
/// Shared across all tests in the "Integration" collection.
/// </summary>
public class InventoryIntegrationFixture : IAsyncLifetime
{
    public PostgresContainerFixture Postgres { get; } = new();
    public WireMockServer WireMock { get; private set; } = null!;
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
        WireMock = WireMockServer.Start();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                // Set configuration via environment variables — these are read by
                // ConfigurationManager during Program.cs execution, BEFORE any
                // WebApplicationFactory callbacks run
                Environment.SetEnvironmentVariable("ConnectionStrings__erp-inventory", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("ConnectionStrings__kafka", "localhost:9092");
                Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", "amqp://guest:guest@localhost:5672");
                Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", Postgres.ConnectionString);
                Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtTokenHelper.TestSecret);
                Environment.SetEnvironmentVariable("Jwt__Issuer", TestJwtTokenHelper.TestIssuer);
                Environment.SetEnvironmentVariable("Jwt__Audience", TestJwtTokenHelper.TestAudience);
                Environment.SetEnvironmentVariable("Minio__Endpoint", "localhost:9002");
                Environment.SetEnvironmentVariable("Minio__AccessKey", "minioadmin");
                Environment.SetEnvironmentVariable("Minio__SecretKey", "minioadmin");

                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with Testcontainers Postgres
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<DbContext>();
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<IDbContextFactory<AppDbContext>>();

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(Postgres.ConnectionString)
                            .UseSnakeCaseNamingConvention());

                    services.AddDbContextFactory<AppDbContext>(options =>
                        options.UseNpgsql(Postgres.ConnectionString)
                            .UseSnakeCaseNamingConvention());

                    // Replace MassTransit with in-memory test harness + no-op Kafka producers
                    // Remove ALL MassTransit-related services (bus, riders, hosted services)
                    // to prevent any connection attempts to RabbitMQ/Kafka
                    var toRemove = services
                        .Where(d =>
                            (d.ServiceType.FullName?.Contains("MassTransit") == true) ||
                            (d.ImplementationType?.FullName?.Contains("MassTransit") == true) ||
                            (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition() == typeof(ITopicProducer<>)))
                        .ToList();
                    foreach (var d in toRemove) services.Remove(d);

                    // Register mock topic producers that the services depend on
                    services.AddSingleton<ITopicProducer<DomainEvents.ProductCreated>>(_ => Mock.Of<ITopicProducer<DomainEvents.ProductCreated>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.ProductUpdated>>(_ => Mock.Of<ITopicProducer<DomainEvents.ProductUpdated>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.ProductDeleted>>(_ => Mock.Of<ITopicProducer<DomainEvents.ProductDeleted>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.StockUpdated>>(_ => Mock.Of<ITopicProducer<DomainEvents.StockUpdated>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.LowStockAlert>>(_ => Mock.Of<ITopicProducer<DomainEvents.LowStockAlert>>());
                    services.AddSingleton<ITopicProducer<DomainEvents.StockMovementCreated>>(_ => Mock.Of<ITopicProducer<DomainEvents.StockMovementCreated>>());

                    // Add MassTransit in-memory test harness (provides IBus, IPublishEndpoint, etc.)
                    services.AddMassTransitTestHarness();

                    // Replace Financial service client with a mock (no real Financial service needed)
                    services.RemoveAll<IFinancialServiceClient>();
                    services.AddSingleton<IFinancialServiceClient>(new StubFinancialServiceClient());

                    // Replace MinIO with a no-op stub
                    services.RemoveAll<IFileStorageService>();
                    services.AddSingleton<IFileStorageService, NoOpFileStorageService>();

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

        // Ignore MassTransit outbox tables when resetting
        DbResetter = new DatabaseResetter(
            Postgres.ConnectionString,
            "__EFMigrationsHistory", "inbox_state", "outbox_message", "outbox_state");
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();
        WireMock?.Stop();
        WireMock?.Dispose();

        // Clean up environment variables
        Environment.SetEnvironmentVariable("ConnectionStrings__erp-inventory", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__kafka", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", null);
        Environment.SetEnvironmentVariable("PostgreSQL__ConnectionString", null);
        Environment.SetEnvironmentVariable("Jwt__Secret", null);
        Environment.SetEnvironmentVariable("Jwt__Issuer", null);
        Environment.SetEnvironmentVariable("Jwt__Audience", null);
        Environment.SetEnvironmentVariable("Minio__Endpoint", null);
        Environment.SetEnvironmentVariable("Minio__AccessKey", null);
        Environment.SetEnvironmentVariable("Minio__SecretKey", null);

        await Postgres.DisposeAsync();
    }
}

/// <summary>
/// No-op file storage for integration tests that don't need MinIO.
/// </summary>
internal class NoOpFileStorageService : IFileStorageService
{
    public Task UploadAsync(string bucket, string objectKey, Stream stream, string contentType, long size) => Task.CompletedTask;
    public Task DeleteAsync(string bucket, string objectKey) => Task.CompletedTask;
    public Task<string> GeneratePresignedDownloadUrlAsync(string bucket, string objectKey, int expirySeconds = 300) => Task.FromResult($"http://localhost/{bucket}/{objectKey}");
    public string GetPublicUrl(string bucket, string objectKey) => $"http://localhost/{bucket}/{objectKey}";
    public Task EnsureBucketsExistAsync() => Task.CompletedTask;
}

/// <summary>
/// Stub Financial service client that returns dummy account IDs.
/// </summary>
internal class StubFinancialServiceClient : IFinancialServiceClient
{
    public Task<string?> GetUserAccountIdAsync(string userId, string authToken) => Task.FromResult<string?>("stub-account-id");
    public Task<string?> GetUserExpenseAccountIdAsync(string userId, string authToken) => Task.FromResult<string?>("stub-expense-id");
    public Task<string?> GetAccountIdByNumberAsync(string accountNumber, string authToken) => Task.FromResult<string?>("stub-account-id");
    public Task<string?> GetAccountIdByNameAsync(string accountName, string authToken) => Task.FromResult<string?>("stub-account-id");
    public Task<string?> GetRevenueAccountIdAsync(string authToken) => Task.FromResult<string?>("stub-revenue-id");
    public Task<string?> GetSystemAccountIdAsync(string purpose, string authToken) => Task.FromResult<string?>("stub-system-id");
    public Task<bool> CreateTransactionAsync(CreateFinancialTransactionRequest request, string authToken) => Task.FromResult(true);
}
