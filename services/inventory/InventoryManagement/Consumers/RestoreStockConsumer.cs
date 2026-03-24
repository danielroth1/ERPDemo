using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using ERP.Contracts.Infrastructure;
using InventoryManagement.Infrastructure;
using InventoryManagement.Services;

namespace InventoryManagement.Consumers;

public class RestoreStockConsumer : IConsumer<RestoreStock>
{
    private readonly ProductService _productService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RestoreStockConsumer> _logger;

    public RestoreStockConsumer(
        ProductService productService,
        AppDbContext dbContext,
        ILogger<RestoreStockConsumer> logger)
    {
        _productService = productService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RestoreStock> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Restoring stock for product {ProductId}, quantity {Quantity}, correlation {CorrelationId}",
            msg.ProductId, msg.Quantity, msg.CorrelationId);

        // Idempotency: check if already processed
        var existing = await _dbContext.ProcessedMessages
            .FirstOrDefaultAsync(m => m.CorrelationId == msg.CorrelationId && m.ConsumerName == nameof(RestoreStockConsumer));

        if (existing != null)
        {
            _logger.LogWarning("Duplicate RestoreStock for correlation {CorrelationId}, re-publishing result", msg.CorrelationId);
            if (existing.ResponseData != null)
                await context.Publish(JsonSerializer.Deserialize<StockRestored>(existing.ResponseData)!);
            return;
        }

        var product = await _productService.GetByIdAsync(msg.ProductId);
        if (product == null)
        {
            _logger.LogError("Cannot restore stock: product {ProductId} not found", msg.ProductId);
            _dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                CorrelationId = msg.CorrelationId,
                ConsumerName = nameof(RestoreStockConsumer),
                Success = false
            });
            await _dbContext.SaveChangesAsync();
            return;
        }

        product.StockQuantity += msg.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        var successEvent = new StockRestored
        {
            CorrelationId = msg.CorrelationId,
            ProductId = msg.ProductId,
            Quantity = msg.Quantity,
            NewStock = product.StockQuantity
        };

        // Atomically save stock change + idempotency record
        _dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            CorrelationId = msg.CorrelationId,
            ConsumerName = nameof(RestoreStockConsumer),
            Success = true,
            ResponseData = JsonSerializer.Serialize(successEvent)
        });
        await _dbContext.SaveChangesAsync();

        await context.Publish(successEvent);

        _logger.LogInformation("Stock restored for product {ProductName}: +{Quantity}, new stock {Stock}",
            product.Name, msg.Quantity, product.StockQuantity);
    }
}
