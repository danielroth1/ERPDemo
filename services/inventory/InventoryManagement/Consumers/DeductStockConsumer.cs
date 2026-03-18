using MassTransit;
using ERP.Contracts.Commands;
using ERP.Contracts.Events;
using InventoryManagement.Services;

namespace InventoryManagement.Consumers;

public class DeductStockConsumer : IConsumer<DeductStock>
{
    private readonly ProductService _productService;
    private readonly ITopicProducer<StockDeducted> _stockDeductedProducer;
    private readonly ITopicProducer<StockDeductionFailed> _stockDeductionFailedProducer;
    private readonly ILogger<DeductStockConsumer> _logger;

    public DeductStockConsumer(
        ProductService productService,
        ITopicProducer<StockDeducted> stockDeductedProducer,
        ITopicProducer<StockDeductionFailed> stockDeductionFailedProducer,
        ILogger<DeductStockConsumer> logger)
    {
        _productService = productService;
        _stockDeductedProducer = stockDeductedProducer;
        _stockDeductionFailedProducer = stockDeductionFailedProducer;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DeductStock> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Deducting stock for product {ProductId}, quantity {Quantity}, correlation {CorrelationId}",
            msg.ProductId, msg.Quantity, msg.CorrelationId);

        var product = await _productService.GetByIdAsync(msg.ProductId);
        if (product == null)
        {
            await _stockDeductionFailedProducer.Produce(new StockDeductionFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = "Product not found during deduction"
            });
            return;
        }

        if (product.StockQuantity < msg.Quantity)
        {
            await _stockDeductionFailedProducer.Produce(new StockDeductionFailed
            {
                CorrelationId = msg.CorrelationId,
                ProductId = msg.ProductId,
                Reason = $"Insufficient stock during deduction. Available: {product.StockQuantity}"
            });
            return;
        }

        product.StockQuantity -= msg.Quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _productService.UpdateAsync(msg.ProductId, product);

        await _stockDeductedProducer.Produce(new StockDeducted
        {
            CorrelationId = msg.CorrelationId,
            ProductId = msg.ProductId,
            ProductName = product.Name,
            QuantityDeducted = msg.Quantity,
            RemainingStock = product.StockQuantity,
            TotalCost = product.Price * msg.Quantity
        });

        _logger.LogInformation("Stock deducted for product {ProductName}: -{Quantity}, remaining {Stock}",
            product.Name, msg.Quantity, product.StockQuantity);
    }
}
