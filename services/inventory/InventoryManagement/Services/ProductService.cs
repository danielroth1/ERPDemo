using Microsoft.EntityFrameworkCore;
using MassTransit;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;
using InventoryManagement.Models.DTOs;
using DomainEvents = ERP.Contracts.Events.Domain;

namespace InventoryManagement.Services;

public class ProductService
{
    private readonly AppDbContext _dbContext;
    private readonly ITopicProducer<DomainEvents.ProductCreated> _productCreatedProducer;
    private readonly ITopicProducer<DomainEvents.ProductUpdated> _productUpdatedProducer;
    private readonly ITopicProducer<DomainEvents.ProductDeleted> _productDeletedProducer;
    private readonly ITopicProducer<DomainEvents.StockUpdated> _stockUpdatedProducer;
    private readonly ITopicProducer<DomainEvents.LowStockAlert> _lowStockAlertProducer;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        AppDbContext dbContext,
        ITopicProducer<DomainEvents.ProductCreated> productCreatedProducer,
        ITopicProducer<DomainEvents.ProductUpdated> productUpdatedProducer,
        ITopicProducer<DomainEvents.ProductDeleted> productDeletedProducer,
        ITopicProducer<DomainEvents.StockUpdated> stockUpdatedProducer,
        ITopicProducer<DomainEvents.LowStockAlert> lowStockAlertProducer,
        ILogger<ProductService> logger)
    {
        _dbContext = dbContext;
        _productCreatedProducer = productCreatedProducer;
        _productUpdatedProducer = productUpdatedProducer;
        _productDeletedProducer = productDeletedProducer;
        _stockUpdatedProducer = stockUpdatedProducer;
        _lowStockAlertProducer = lowStockAlertProducer;
        _logger = logger;
    }

    public async Task<List<Product>> GetAllAsync(int page = 1, int pageSize = 20, bool? isActive = null)
    {
        var query = _dbContext.Products.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(bool? isActive = null)
    {
        var query = _dbContext.Products.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        return await query.CountAsync();
    }

    public async Task<Product?> GetByIdAsync(string id)
    {
        return await _dbContext.Products.FindAsync(id);
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Sku.ToLower() == sku.ToLower());
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        return await _dbContext.Products
            .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
            .ToListAsync();
    }

    public async Task<List<Product>> GetByCategoryIdAsync(string categoryId)
    {
        return await _dbContext.Products
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<int> GetCountByCategoryIdAsync(string categoryId)
    {
        return await _dbContext.Products
            .CountAsync(p => p.CategoryId == categoryId);
    }

    public async Task<List<Product>> SearchAsync(string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        return await _dbContext.Products
            .Where(p => p.Name.ToLower().Contains(lowerSearchTerm) || 
                       p.Sku.ToLower().Contains(lowerSearchTerm))
            .Take(50)
            .ToListAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        product.Id = Guid.NewGuid().ToString();
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        await _productCreatedProducer.Produce(new DomainEvents.ProductCreated
        {
            ProductId = product.Id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        });

        return product;
    }

    public async Task<bool> UpdateAsync(string id, Product product)
    {
        var existingProduct = await _dbContext.Products.FindAsync(id);
        if (existingProduct == null) return false;

        existingProduct.Sku = product.Sku;
        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.CategoryId = product.CategoryId;
        existingProduct.Price = product.Price;
        existingProduct.Cost = product.Cost;
        existingProduct.StockQuantity = product.StockQuantity;
        existingProduct.MinStockLevel = product.MinStockLevel;
        existingProduct.MaxStockLevel = product.MaxStockLevel;
        existingProduct.Unit = product.Unit;
        existingProduct.IsActive = product.IsActive;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Product updated: {ProductId}", id);

        await _productUpdatedProducer.Produce(new DomainEvents.ProductUpdated
        {
            ProductId = id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        });

        return true;
    }

    public async Task<bool> UpdateStockAsync(string id, int newQuantity, string userId)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null) return false;

        product.StockQuantity = newQuantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Stock updated for product: {ProductId}, new quantity: {Quantity}", id, newQuantity);

        await _stockUpdatedProducer.Produce(new DomainEvents.StockUpdated
        {
            ProductId = id,
            ProductName = product.Name,
            OldQuantity = product.StockQuantity,
            NewQuantity = newQuantity
        });

        // Check for low stock alert
        if (product.IsLowStock)
        {
            await _lowStockAlertProducer.Produce(new DomainEvents.LowStockAlert
            {
                ProductId = product.Id,
                ProductName = product.Name,
                CurrentStock = product.StockQuantity,
                ReorderLevel = product.MinStockLevel
            });
        }

        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var product = await _dbContext.Products.FindAsync(id);
        if (product == null) return false;

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Product deleted: {ProductId}", id);

        await _productDeletedProducer.Produce(new DomainEvents.ProductDeleted { ProductId = id });

        return true;
    }

    public async Task<ProductResponse> MapToResponse(Product product)
    {
        var category = !string.IsNullOrEmpty(product.CategoryId)
            ? await _dbContext.Categories.FindAsync(product.CategoryId)
            : null;

        var documents = await _dbContext.ProductDocuments
            .Where(d => d.ProductId == product.Id)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new ProductDocumentDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                OriginalFileName = d.OriginalFileName,
                ContentType = d.ContentType,
                SizeBytes = d.SizeBytes,
                UploadedBy = d.UploadedBy,
                UploadedAt = d.UploadedAt
            })
            .ToListAsync();

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
            ImageUrl = product.ImageUrl,
            Documents = documents,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _dbContext.Categories.ToListAsync();
    }

    public async Task<Category> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Category created: {CategoryId} - {CategoryName}", category.Id, category.Name);

        return category;
    }

    public async Task<Product> CreateAsync(CreateProductRequest request)
    {
        var product = new Product
        {
            Id = Guid.NewGuid().ToString(),
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

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductId} - {ProductName}", product.Id, product.Name);

        await _productCreatedProducer.Produce(new DomainEvents.ProductCreated
        {
            ProductId = product.Id,
            Name = product.Name,
            CategoryId = product.CategoryId,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        });

        return product;
    }
}

