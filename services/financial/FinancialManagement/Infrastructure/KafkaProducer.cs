using Confluent.Kafka;
using FinancialManagement.Configuration;
using System.Text.Json;

namespace FinancialManagement.Infrastructure;

public class KafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(KafkaSettings settings, ILogger<KafkaProducer> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MaxInFlight = 5,
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }

    public async Task PublishAsync<T>(string topic, string entityId, string eventType, T data)
    {
        try
        {
            var envelope = new
            {
                EventType = eventType,
                Timestamp = DateTime.UtcNow,
                Data = data
            };
            var json = JsonSerializer.Serialize(envelope);
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = entityId,
                Value = json,
                Timestamp = new Timestamp(DateTime.UtcNow)
            });

            _logger.LogInformation("Published event {EventType} to {Topic} with key {Key} at offset {Offset}",
                eventType, topic, entityId, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to {Topic} with key {Key}", eventType, topic, entityId);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
