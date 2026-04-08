using Testcontainers.PostgreSql;
using Xunit;

namespace ERP.Testing.Shared.Fixtures;

/// <summary>
/// Shared PostgreSQL container fixture. Use as an xUnit ICollectionFixture
/// so a single container is shared across all tests in a collection.
/// </summary>
public class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test_db")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
