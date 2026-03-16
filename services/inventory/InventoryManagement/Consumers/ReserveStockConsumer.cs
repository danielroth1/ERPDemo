using MassTransit;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using InventoryManagement.Services;

namespace InventoryManagement.Consumers;

public class ReserveStockConsumer : IConsumer<ReserveStock>
{
    private readonly ProductService _productService;
    private readonly ILogger<ReserveStockConsumer> _logger;

    public ReserveStockConsumer(ProductService productService, ILogger<ReserveStockConsumer> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReserveStock> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Reserving stock for product {ProductId}, quantity {Quantity}, correlation {CorrelationId}",
            msg.ProductId, msg.Quantity, msg.CorrelationId);

        var product = await _productService.GetByIdAsync(msg.ProductId);
        if (product == null)
        {
            await context.Publish(new StockReservationFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = "Product not found"
            });
            return;
        }

        if (!product.IsActive)
        {
            await context.Publish(new StockReservationFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = "Product is not available"
            });
            return;
        }

        if (product.StockQuantity < msg.Quantity)
        {
            await context.Publish(new StockReservationFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = $"Insufficient stock. Available: {product.StockQuantity}"
            });
            return;
        }

        await context.Publish(new StockReserved
        {
            CorrelationId = msg.CorrelationId,
            ProductId = msg.ProductId,
            ProductName = product.Name,
            Quantity = msg.Quantity,
            UnitPrice = product.Price,
            RemainingStock = product.StockQuantity
        });

        _logger.LogInformation("Stock reserved for product {ProductName} ({ProductId}), qty {Quantity}",
            product.Name, msg.ProductId, msg.Quantity);
    }
}
