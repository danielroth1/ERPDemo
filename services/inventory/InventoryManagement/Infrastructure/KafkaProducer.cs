using Confluent.Kafka;
using InventoryManagement.Configuration;
using System.Text.Json;

namespace InventoryManagement.Infrastructure;

public class KafkaProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(KafkaSettings settings, ILogger<KafkaProducer> logger)
    {
        _settings = settings;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishEventAsync<T>(string eventType, T data)
    {
        try
        {
            var message = new
            {
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Data = data
            };

            var json = JsonSerializer.Serialize(message);

            var result = await _producer.ProduceAsync(
                _settings.Topic,
                new Message<string, string>
                {
                    Key = eventType,
                    Value = json
                });

            _logger.LogInformation(
                "Published event {EventType} to Kafka topic {Topic} at partition {Partition}, offset {Offset}",
                eventType, _settings.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType} to Kafka", eventType);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
