using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using ERP.Contracts.Commands;
using ERP.Contracts.Infrastructure;
using InventoryManagement.Infrastructure;
using InventoryManagement.Services;

namespace InventoryManagement.Consumers;

public class ReleaseReservationConsumer : IConsumer<ReleaseReservation>
{
    private readonly ProductService _productService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReleaseReservationConsumer> _logger;

    public ReleaseReservationConsumer(
        ProductService productService,
        AppDbContext dbContext,
        ILogger<ReleaseReservationConsumer> logger)
    {
        _productService = productService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReleaseReservation> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Releasing reservation for product {ProductId}, quantity {Quantity}, correlation {CorrelationId}",
            msg.ProductId, msg.Quantity, msg.CorrelationId);

        // Idempotency: check if already processed
        var existing = await _dbContext.ProcessedMessages
            .FirstOrDefaultAsync(m => m.CorrelationId == msg.CorrelationId && m.ConsumerName == nameof(ReleaseReservationConsumer));

        if (existing != null)
        {
            _logger.LogWarning("Duplicate ReleaseReservation for correlation {CorrelationId}, skipping", msg.CorrelationId);
            return;
        }

        var product = await _productService.GetByIdAsync(msg.ProductId);
        if (product == null)
        {
            _logger.LogError("Cannot release reservation: product {ProductId} not found", msg.ProductId);
            _dbContext.ProcessedMessages.Add(new ProcessedMessage
            {
                CorrelationId = msg.CorrelationId,
                ConsumerName = nameof(ReleaseReservationConsumer),
                Success = false
            });
            await _dbContext.SaveChangesAsync();
            return;
        }

        product.ReservedQuantity = Math.Max(0, product.ReservedQuantity - msg.Quantity);
        product.UpdatedAt = DateTime.UtcNow;

        _dbContext.ProcessedMessages.Add(new ProcessedMessage
        {
            CorrelationId = msg.CorrelationId,
            ConsumerName = nameof(ReleaseReservationConsumer),
            Success = true
        });
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Reservation released for product {ProductId}, qty {Quantity}, reserved now {Reserved}",
            msg.ProductId, msg.Quantity, product.ReservedQuantity);
    }
}
