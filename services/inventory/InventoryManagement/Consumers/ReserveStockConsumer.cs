using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using ERP.Contracts.Infrastructure;
using InventoryManagement.Infrastructure;
using InventoryManagement.Services;

namespace InventoryManagement.Consumers;

public class ReserveStockConsumer : IConsumer<ReserveStock>
{
    private readonly ProductService _productService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReserveStockConsumer> _logger;

    public ReserveStockConsumer(
        ProductService productService,
        AppDbContext dbContext,
        ILogger<ReserveStockConsumer> logger)
    {
        _productService = productService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var timer = new OperationTimer(_logger, "ReserveStock");
        var msg = context.Message;
        _logger.LogInformation("Reserving stock for product {ProductId}, quantity {Quantity}, correlation {CorrelationId}",
            msg.ProductId, msg.Quantity, msg.CorrelationId);

        // Idempotency: check if already processed
        ProcessedMessage? existing;
        using (timer.Step("IdempotencyCheck"))
        {
            existing = await _dbContext.ProcessedMessages
                .FirstOrDefaultAsync(m => m.CorrelationId == msg.CorrelationId && m.ConsumerName == nameof(ReserveStockConsumer));
        }

        if (existing != null)
        {
            _logger.LogWarning("Duplicate ReserveStock for correlation {CorrelationId}, re-publishing result", msg.CorrelationId);
            if (existing.Success && existing.ResponseData != null)
                await context.Publish(JsonSerializer.Deserialize<StockReserved>(existing.ResponseData)!);
            else if (!existing.Success && existing.ResponseData != null)
                await context.Publish(JsonSerializer.Deserialize<StockReservationFailed>(existing.ResponseData)!);
            timer.LogSummary();
            return;
        }

        InventoryManagement.Models.Product? product;
        using (timer.Step("GetProduct"))
        {
            product = await _productService.GetByIdAsync(msg.ProductId);
        }
        if (product == null)
        {
            var failEvent = new StockReservationFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = "Product not found"
            };
            _dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                CorrelationId = msg.CorrelationId,
                ConsumerName = nameof(ReserveStockConsumer),
                Success = false,
                ResponseData = JsonSerializer.Serialize(failEvent)
            });
            await _dbContext.SaveChangesAsync();
            await context.Publish(failEvent);
            return;
        }

        if (!product.IsActive)
        {
            var failEvent = new StockReservationFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = "Product is not available"
            };
            _dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                CorrelationId = msg.CorrelationId,
                ConsumerName = nameof(ReserveStockConsumer),
                Success = false,
                ResponseData = JsonSerializer.Serialize(failEvent)
            });
            await _dbContext.SaveChangesAsync();
            await context.Publish(failEvent);
            return;
        }

        if (product.AvailableQuantity < msg.Quantity)
        {
            var failEvent = new StockReservationFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = $"Insufficient stock. Available: {product.AvailableQuantity}"
            };
            _dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                CorrelationId = msg.CorrelationId,
                ConsumerName = nameof(ReserveStockConsumer),
                Success = false,
                ResponseData = JsonSerializer.Serialize(failEvent)
            });
            await _dbContext.SaveChangesAsync();
            await context.Publish(failEvent);
            return;
        }

        // Reserve stock atomically
        product.ReservedQuantity += msg.Quantity;
        product.UpdatedAt = DateTime.UtcNow;

        var successEvent = new StockReserved
        {
            CorrelationId = msg.CorrelationId,
            ProductId = msg.ProductId,
            ProductName = product.Name,
            Quantity = msg.Quantity,
            UnitPrice = product.Price,
            RemainingStock = product.AvailableQuantity
        };

        _dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            CorrelationId = msg.CorrelationId,
            ConsumerName = nameof(ReserveStockConsumer),
            Success = true,
            ResponseData = JsonSerializer.Serialize(successEvent)
        });
        using (timer.Step("SaveChanges"))
        {
            await _dbContext.SaveChangesAsync();
        }

        using (timer.Step("PublishStockReserved"))
        {
            await context.Publish(successEvent);
        }

        timer.LogSummary();
        _logger.LogInformation("Stock reserved for product {ProductName} ({ProductId}), qty {Quantity}, reserved {Reserved}, available {Available}",
            product.Name, msg.ProductId, msg.Quantity, product.ReservedQuantity, product.AvailableQuantity);
    }
}
