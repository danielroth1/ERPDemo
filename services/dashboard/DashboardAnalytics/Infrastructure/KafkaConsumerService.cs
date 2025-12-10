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

        // Create consumers for each topic
        var topics = new[]
        {
            "users.user.created",
            "users.user.updated",
            "inventory.product.created",
            "inventory.product.updated",
            "inventory.stock.low",
            "sales.order.created",
            "sales.order.updated",
            "sales.invoice.paid",
            "financial.transaction.created",
            "financial.budget.exceeded"
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
                    _logger.LogInformation("Received message from topic {Topic}: {Message}", topic, result.Message.Value);

                    using var scope = _serviceProvider.CreateScope();
                    var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

                    await ProcessMessage(topic, result.Message.Value, analyticsService);
                    
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

    private async Task ProcessMessage(string topic, string message, IAnalyticsService analyticsService)
    {
        try
        {
            switch (topic)
            {
                case "users.user.created":
                case "users.user.updated":
                    var userEvent = JsonSerializer.Deserialize<UserEventDTO>(message);
                    if (userEvent != null)
                        await analyticsService.ProcessUserEventAsync(userEvent);
                    break;

                case "inventory.product.created":
                case "inventory.product.updated":
                    var productEvent = JsonSerializer.Deserialize<ProductEventDTO>(message);
                    if (productEvent != null)
                        await analyticsService.ProcessProductEventAsync(productEvent);
                    break;

                case "inventory.stock.low":
                    var lowStockEvent = JsonSerializer.Deserialize<ProductEventDTO>(message);
                    if (lowStockEvent != null)
                        await analyticsService.ProcessLowStockAlertAsync(lowStockEvent);
                    break;

                case "sales.order.created":
                case "sales.order.updated":
                case "sales.invoice.paid":
                    var orderEvent = JsonSerializer.Deserialize<OrderEventDTO>(message);
                    if (orderEvent != null)
                        await analyticsService.ProcessOrderEventAsync(orderEvent);
                    break;

                case "financial.transaction.created":
                    var transactionEvent = JsonSerializer.Deserialize<TransactionEventDTO>(message);
                    if (transactionEvent != null)
                        await analyticsService.ProcessTransactionEventAsync(transactionEvent);
                    break;

                case "financial.budget.exceeded":
                    var budgetEvent = JsonSerializer.Deserialize<BudgetEventDTO>(message);
                    if (budgetEvent != null)
                        await analyticsService.ProcessBudgetExceededAlertAsync(budgetEvent);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from topic {Topic}: {Message}", topic, message);
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
