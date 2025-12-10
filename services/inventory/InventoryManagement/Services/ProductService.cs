using MongoDB.Driver;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;
using InventoryManagement.Models.DTOs;

namespace InventoryManagement.Services;

public class ProductService
{
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Category> _categories;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        MongoDbContext dbContext,
        KafkaProducer kafkaProducer,
        ILogger<ProductService> logger)
    {
        _products = dbContext.GetCollection<Product>("products");
        _categories = dbContext.GetCollection<Category>("categories");
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllAsync(int page = 1, int pageSize = 20, bool? isActive = null)
    {
        var filter = isActive.HasValue
            ? Builders<Product>.Filter.Eq(p => p.IsActive, isActive.Value)
            : Builders<Product>.Filter.Empty;

        return await _products
            .Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(bool? isActive = null)
    {
        var filter = isActive.HasValue
            ? Builders<Product>.Filter.Eq(p => p.IsActive, isActive.Value)
            : Builders<Product>.Filter.Empty;

        return (int)await _products.CountDocumentsAsync(filter);
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _products.Find(p => p.Sku.ToLower() == sku.ToLower()).FirstOrDefaultAsync();
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        var filter = Builders<Product>.Filter.Where(p =>
            p.IsActive && p.StockQuantity <= p.MinStockLevel);

        return await _products.Find(filter).ToListAsync();
    }

    public async Task<List<Product>> GetByCategoryIdAsync(string categoryId)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.CategoryId, categoryId);
        return await _products.Find(filter).ToListAsync();
    }

    public async Task<int> GetCountByCategoryIdAsync(string categoryId)
    {
        var filter = Builders<Product>.Filter.Eq(p => p.CategoryId, categoryId);
        return (int)await _products.CountDocumentsAsync(filter);
    }

    public async Task<List<Product>> SearchAsync(string searchTerm)
    {
        var filter = Builders<Product>.Filter.Or(
            Builders<Product>.Filter.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
            Builders<Product>.Filter.Regex(p => p.Sku, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
        );

        return await _products.Find(filter).Limit(50).ToListAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        await _products.InsertOneAsync(product);

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        await _kafkaProducer.PublishEventAsync("ProductCreated", new
        {
            product.Id,
            product.Sku,
            product.Name,
            product.Price,
            product.StockQuantity
        });

        return product;
    }

    public async Task<bool> UpdateAsync(string id, Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;

        var result = await _products.ReplaceOneAsync(p => p.Id == id, product);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Product updated: {ProductId}", id);

            await _kafkaProducer.PublishEventAsync("ProductUpdated", new
            {
                product.Id,
                product.Name,
                product.Price,
                product.StockQuantity,
                product.IsActive
            });

            return true;
        }

        return false;
    }

    public async Task<bool> UpdateStockAsync(string id, int newQuantity, string userId)
    {
        var update = Builders<Product>.Update
            .Set(p => p.StockQuantity, newQuantity)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        var result = await _products.UpdateOneAsync(p => p.Id == id, update);

        if (result.ModifiedCount > 0)
        {
            var product = await GetByIdAsync(id);

            _logger.LogInformation("Stock updated for product: {ProductId}, new quantity: {Quantity}", id, newQuantity);

            await _kafkaProducer.PublishEventAsync("StockUpdated", new
            {
                ProductId = id,
                NewQuantity = newQuantity,
                IsLowStock = product?.IsLowStock ?? false,
                UpdatedBy = userId
            });

            // Check for low stock alert
            if (product != null && product.IsLowStock)
            {
                await _kafkaProducer.PublishEventAsync("LowStockAlert", new
                {
                    product.Id,
                    product.Name,
                    product.Sku,
                    CurrentStock = product.StockQuantity,
                    MinLevel = product.MinStockLevel
                });
            }

            return true;
        }

        return false;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _products.DeleteOneAsync(p => p.Id == id);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Product deleted: {ProductId}", id);

            await _kafkaProducer.PublishEventAsync("ProductDeleted", new { ProductId = id });

            return true;
        }

        return false;
    }

    public async Task<ProductResponse> MapToResponse(Product product)
    {
        var category = !string.IsNullOrEmpty(product.CategoryId)
            ? await _categories.Find(c => c.Id == product.CategoryId).FirstOrDefaultAsync()
            : null;

        return new ProductResponse
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            CategoryId = product.CategoryId,
            CategoryName = category?.Name ?? "",
            Price = product.Price,
            Cost = product.Cost,
            StockQuantity = product.StockQuantity,
            MinStockLevel = product.MinStockLevel,
            MaxStockLevel = product.MaxStockLevel,
            Unit = product.Unit,
            IsActive = product.IsActive,
            IsLowStock = product.IsLowStock,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _categories.Find(_ => true).ToListAsync();
    }

    public async Task<Category> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _categories.InsertOneAsync(category);

        _logger.LogInformation("Category created: {CategoryId} - {CategoryName}", category.Id, category.Name);

        return category;
    }

    public async Task<Product> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Price = request.Price,
            Cost = request.Price * 0.6m, // Default cost as 60% of price
            StockQuantity = request.Quantity,
            MinStockLevel = 10,
            MaxStockLevel = 1000,
            Unit = "pcs",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _products.InsertOneAsync(product);

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        await _kafkaProducer.PublishEventAsync("ProductCreated", new
        {
            product.Id,
            product.Sku,
            product.Name,
            product.Price,
            product.StockQuantity
        });

        return product;
    }
}

