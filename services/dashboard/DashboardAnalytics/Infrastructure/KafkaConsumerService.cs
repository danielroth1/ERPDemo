using Confluent.Kafka;
using DashboardAnalytics.Configuration;
using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DashboardAnalytics.Infrastructure;

public class KafkaConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly List<IConsumer<string, string>> _consumers = new();

    public KafkaConsumerService(
        IServiceProvider serviceProvider,
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<KafkaConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        // One consumer per service topic — event type is identified from the message body
        var topics = new[]
        {
            "user-events",
            "inventory-events",
            "sales-events",
            "financial-events"
        };

        foreach (var topic in topics)
        {
            var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(topic);
            _consumers.Add(consumer);

            _ = Task.Run(async () => await ConsumeMessages(consumer, topic, stoppingToken), stoppingToken);
        }

        await Task.CompletedTask;
    }

    private async Task ConsumeMessages(IConsumer<string, string> consumer, string topic, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result != null)
                {
                    var eventType = ExtractEventType(result.Message.Value);
                    _logger.LogInformation("Received event {EventType} from topic {Topic}", eventType, topic);

                    using var scope = _serviceProvider.CreateScope();
                    var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

                    await ProcessMessage(eventType, result.Message.Value, analyticsService);

                    consumer.Commit(result);
                }
            }
            catch (ConsumeException e)
            {
                _logger.LogError(e, "Error consuming message from topic {Topic}", topic);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing message from topic {Topic}", topic);
            }
        }
    }

    private static string ExtractEventType(string message)
    {
        try
        {
            using var doc = JsonDocument.Parse(message);
            if (doc.RootElement.TryGetProperty("EventType", out var prop))
                return prop.GetString() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }

    private async Task ProcessMessage(string eventType, string message, IAnalyticsService analyticsService)
    {
        try
        {
            switch (eventType)
            {
                case "UserCreated":
                case "UserUpdated":
                    var userEvent = JsonSerializer.Deserialize<UserEventDTO>(message);
                    if (userEvent != null)
                        await analyticsService.ProcessUserEventAsync(userEvent);
                    break;

                case "ProductCreated":
                case "ProductUpdated":
                    var productEvent = JsonSerializer.Deserialize<ProductEventDTO>(message);
                    if (productEvent != null)
                        await analyticsService.ProcessProductEventAsync(productEvent);
                    break;

                case "LowStockAlert":
                    var lowStockEvent = JsonSerializer.Deserialize<ProductEventDTO>(message);
                    if (lowStockEvent != null)
                        await analyticsService.ProcessLowStockAlertAsync(lowStockEvent);
                    break;

                case "OrderCreated":
                case "OrderStatusChanged":
                case "InvoiceCreated":
                case "InvoicePaid":
                    var orderEvent = JsonSerializer.Deserialize<OrderEventDTO>(message);
                    if (orderEvent != null)
                        await analyticsService.ProcessOrderEventAsync(orderEvent);
                    break;

                case "TransactionCreated":
                    var transactionEvent = JsonSerializer.Deserialize<TransactionEventDTO>(message);
                    if (transactionEvent != null)
                        await analyticsService.ProcessTransactionEventAsync(transactionEvent);
                    break;

                case "BudgetExceeded":
                    var budgetEvent = JsonSerializer.Deserialize<BudgetEventDTO>(message);
                    if (budgetEvent != null)
                        await analyticsService.ProcessBudgetExceededAlertAsync(budgetEvent);
                    break;

                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {EventType}: {Message}", eventType, message);
        }
    }

    public override void Dispose()
    {
        foreach (var consumer in _consumers)
        {
            consumer.Close();
            consumer.Dispose();
        }
        base.Dispose();
    }
}
