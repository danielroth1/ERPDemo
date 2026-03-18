using MassTransit;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using InventoryManagement.Services;

namespace InventoryManagement.Consumers;

public class RestoreStockConsumer : IConsumer<RestoreStock>
{
    private readonly ProductService _productService;
    private readonly ITopicProducer<StockRestored> _stockRestoredProducer;
    private readonly ILogger<RestoreStockConsumer> _logger;

    public RestoreStockConsumer(
        ProductService productService,
        ITopicProducer<StockRestored> stockRestoredProducer,
        ILogger<RestoreStockConsumer> logger)
    {
        _productService = productService;
        _stockRestoredProducer = stockRestoredProducer;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RestoreStock> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Restoring stock for product {ProductId}, quantity {Quantity}, correlation {CorrelationId}",
            msg.ProductId, msg.Quantity, msg.CorrelationId);

        var product = await _productService.GetByIdAsync(msg.ProductId);
        if (product == null)
        {
            _logger.LogError("Cannot restore stock: product {ProductId} not found", msg.ProductId);
            return;
        }

        product.StockQuantity += msg.Quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _productService.UpdateAsync(msg.ProductId, product);

        await _stockRestoredProducer.Produce(new StockRestored
        {
            CorrelationId = msg.CorrelationId,
            ProductId = msg.ProductId,
            Quantity = msg.Quantity,
            NewStock = product.StockQuantity
        });

        _logger.LogInformation("Stock restored for product {ProductName}: +{Quantity}, new stock {Stock}",
            product.Name, msg.Quantity, product.StockQuantity);
    }
}
