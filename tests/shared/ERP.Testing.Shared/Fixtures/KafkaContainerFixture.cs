using Testcontainers.Kafka;
using Xunit;

namespace ERP.Testing.Shared.Fixtures;

/// <summary>
/// Shared Kafka container fixture for component tests.
/// </summary>
public class KafkaContainerFixture : IAsyncLifetime
{
    private readonly KafkaContainer _container = new KafkaBuilder()
        .WithImage("confluentinc/cp-kafka:7.5.0")
        .Build();

    public string BootstrapServers => _container.GetBootstrapAddress();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
