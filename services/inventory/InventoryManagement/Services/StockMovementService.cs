using Microsoft.EntityFrameworkCore;
using MassTransit;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;
using ERP.Contracts.Events.Domain;

namespace InventoryManagement.Services;

public class StockMovementService
{
    private readonly AppDbContext _dbContext;
    private readonly ITopicProducer<StockMovementCreated> _stockMovementCreatedProducer;
    private readonly ProductService _productService;
    private readonly ILogger<StockMovementService> _logger;

    public StockMovementService(
        AppDbContext dbContext,
        ITopicProducer<StockMovementCreated> stockMovementCreatedProducer,
        ProductService productService,
        ILogger<StockMovementService> logger)
    {
        _dbContext = dbContext;
        _stockMovementCreatedProducer = stockMovementCreatedProducer;
        _productService = productService;
        _logger = logger;
    }

    public async Task<List<StockMovement>> GetByProductIdAsync(string productId, int limit = 50)
    {
        return await _dbContext.StockMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<StockMovement>> GetRecentMovementsAsync(int limit = 100)
    {
        return await _dbContext.StockMovements
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<StockMovement> CreateAsync(StockMovement movement, string userId)
    {
        movement.Id = Guid.NewGuid().ToString();
        movement.CreatedBy = userId;
        movement.CreatedAt = DateTime.UtcNow;

        _dbContext.StockMovements.Add(movement);
        await _dbContext.SaveChangesAsync();

        // Update product stock
        var product = await _dbContext.Products.FindAsync(movement.ProductId);
        if (product != null)
        {
            int newQuantity = product.StockQuantity;

            switch (movement.MovementType)
            {
                case MovementType.Purchase:
                case MovementType.Return:
                    newQuantity += movement.Quantity;
                    break;
                case MovementType.Sale:
                    newQuantity -= movement.Quantity;
                    break;
                case MovementType.Adjustment:
                    // Quantity represents the adjustment (can be + or -)
                    newQuantity = movement.Quantity;
                    break;
            }

            await _productService.UpdateStockAsync(movement.ProductId, newQuantity, userId);
        }

        _logger.LogInformation(
            "Stock movement created: {MovementId} for product {ProductId}, type: {Type}, quantity: {Quantity}",
            movement.Id, movement.ProductId, movement.MovementType, movement.Quantity);

        await _stockMovementCreatedProducer.Produce(new StockMovementCreated
        {
            MovementId = movement.Id,
            ProductId = movement.ProductId,
            Type = movement.MovementType.ToString(),
            Quantity = movement.Quantity
        });

        return movement;
    }
}
