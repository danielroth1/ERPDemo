using Testcontainers.RabbitMq;
using Xunit;

namespace ERP.Testing.Shared.Fixtures;

/// <summary>
/// Shared RabbitMQ container fixture for component tests.
/// </summary>
public class RabbitMqContainerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-management-alpine")
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
