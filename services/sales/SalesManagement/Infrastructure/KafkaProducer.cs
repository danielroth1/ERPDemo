using Confluent.Kafka;
using SalesManagement.Configuration;
using System.Text.Json;

namespace SalesManagement.Infrastructure;

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

    public async Task PublishAsync<T>(string topic, string key, T message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = json,
                Timestamp = new Timestamp(DateTime.UtcNow)
            });

            _logger.LogInformation("Published message to {Topic} with key {Key} at offset {Offset}", 
                topic, key, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish message to {Topic} with key {Key}", topic, key);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
