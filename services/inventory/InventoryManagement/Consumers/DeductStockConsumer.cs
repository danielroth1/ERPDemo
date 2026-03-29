using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using ERP.Contracts.Infrastructure;
using InventoryManagement.Infrastructure;
using InventoryManagement.Services;

namespace InventoryManagement.Consumers;

public class DeductStockConsumer : IConsumer<DeductStock>
{
    private readonly ProductService _productService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DeductStockConsumer> _logger;

    public DeductStockConsumer(
        ProductService productService,
        AppDbContext dbContext,
        ILogger<DeductStockConsumer> logger)
    {
        _productService = productService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeductStock> context)
    {
        var timer = new OperationTimer(_logger, "DeductStock");
        var msg = context.Message;
        _logger.LogInformation("Deducting stock for product {ProductId}, quantity {Quantity}, correlation {CorrelationId}",
            msg.ProductId, msg.Quantity, msg.CorrelationId);

        // Idempotency: check if already processed
        ProcessedMessage? existing;
        using (timer.Step("IdempotencyCheck"))
        {
            existing = await _dbContext.ProcessedMessages
                .FirstOrDefaultAsync(m => m.CorrelationId == msg.CorrelationId && m.ConsumerName == nameof(DeductStockConsumer));
        }

        if (existing != null)
        {
            _logger.LogWarning("Duplicate DeductStock for correlation {CorrelationId}, re-publishing result", msg.CorrelationId);
            if (existing.Success && existing.ResponseData != null)
                await context.Publish(JsonSerializer.Deserialize<StockDeducted>(existing.ResponseData)!);
            else if (!existing.Success && existing.ResponseData != null)
                await context.Publish(JsonSerializer.Deserialize<StockDeductionFailed>(existing.ResponseData)!);
            return;
        }

        InventoryManagement.Models.Product? product;
        using (timer.Step("GetProduct"))
        {
            product = await _productService.GetByIdAsync(msg.ProductId);
        }
        if (product == null)
        {
            var failEvent = new StockDeductionFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = "Product not found during deduction"
            };
            _dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                CorrelationId = msg.CorrelationId,
                ConsumerName = nameof(DeductStockConsumer),
                Success = false,
                ResponseData = JsonSerializer.Serialize(failEvent)
            });
            await _dbContext.SaveChangesAsync();
            await context.Publish(failEvent);
            return;
        }

        if (product.StockQuantity < msg.Quantity)
        {
            var failEvent = new StockDeductionFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = $"Insufficient stock during deduction. Available: {product.StockQuantity}"
            };
            _dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                CorrelationId = msg.CorrelationId,
                ConsumerName = nameof(DeductStockConsumer),
                Success = false,
                ResponseData = JsonSerializer.Serialize(failEvent)
            });
            await _dbContext.SaveChangesAsync();
            await context.Publish(failEvent);
            return;
        }

        // Deduct from actual stock and release the reservation
        product.StockQuantity -= msg.Quantity;
        product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - msg.Quantity);
        product.UpdatedAt = DateTime.UtcNow;

        var successEvent = new StockDeducted
        {
            CorrelationId = msg.CorrelationId,
            ProductId = msg.ProductId,
            ProductName = product.Name,
            QuantityDeducted = msg.Quantity,
            RemainingStock = product.StockQuantity,
            TotalCost = product.Price * msg.Quantity
        };

        _dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            CorrelationId = msg.CorrelationId,
            ConsumerName = nameof(DeductStockConsumer),
            Success = true,
            ResponseData = JsonSerializer.Serialize(successEvent)
        });
        using (timer.Step("SaveChanges"))
        {
            await _dbContext.SaveChangesAsync();
        }

        using (timer.Step("PublishStockDeducted"))
        {
            await context.Publish(successEvent);
        }

        timer.LogSummary();
        _logger.LogInformation("Stock deducted for product {ProductName}: -{Quantity}, remaining {Stock}",
            product.Name, msg.Quantity, product.StockQuantity);
    }
}
