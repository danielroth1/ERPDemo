using MongoDB.Driver;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;

namespace InventoryManagement.Services;

public class StockMovementService
{
    private readonly IMongoCollection<StockMovement> _movements;
    private readonly IMongoCollection<Product> _products;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ProductService _productService;
    private readonly ILogger<StockMovementService> _logger;

    public StockMovementService(
        MongoDbContext dbContext,
        KafkaProducer kafkaProducer,
        ProductService productService,
        ILogger<StockMovementService> logger)
    {
        _movements = dbContext.GetCollection<StockMovement>("stock_movements");
        _products = dbContext.GetCollection<Product>("products");
        _kafkaProducer = kafkaProducer;
        _productService = productService;
        _logger = logger;
    }

    public async Task<List<StockMovement>> GetByProductIdAsync(string productId, int limit = 50)
    {
        return await _movements
            .Find(m => m.ProductId == productId)
            .SortByDescending(m => m.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<List<StockMovement>> GetRecentMovementsAsync(int limit = 100)
    {
        return await _movements
            .Find(_ => true)
            .SortByDescending(m => m.CreatedAt)
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<StockMovement> CreateAsync(StockMovement movement, string userId)
    {
        movement.CreatedBy = userId;
        movement.CreatedAt = DateTime.UtcNow;

        await _movements.InsertOneAsync(movement);

        // Update product stock
        var product = await _products.Find(p => p.Id == movement.ProductId).FirstOrDefaultAsync();
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

        await _kafkaProducer.PublishEventAsync("StockMovementCreated", new
        {
            movement.Id,
            movement.ProductId,
            movement.MovementType,
            movement.Quantity,
            movement.Reference
        });

        return movement;
    }
}
