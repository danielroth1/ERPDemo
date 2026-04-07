using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;
using InventoryManagement.Services;
using InventoryManagement.Tests.Helpers;
using DomainEvents = ERP.Contracts.Events.Domain;

namespace InventoryManagement.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITopicProducer<DomainEvents.ProductCreated>> _productCreatedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.ProductUpdated>> _productUpdatedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.ProductDeleted>> _productDeletedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.StockUpdated>> _stockUpdatedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.LowStockAlert>> _lowStockAlertProducer;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _productCreatedProducer = new Mock<ITopicProducer<DomainEvents.ProductCreated>>();
        _productUpdatedProducer = new Mock<ITopicProducer<DomainEvents.ProductUpdated>>();
        _productDeletedProducer = new Mock<ITopicProducer<DomainEvents.ProductDeleted>>();
        _stockUpdatedProducer = new Mock<ITopicProducer<DomainEvents.StockUpdated>>();
        _lowStockAlertProducer = new Mock<ITopicProducer<DomainEvents.LowStockAlert>>();
        _loggerMock = new Mock<ILogger<ProductService>>();

        _service = new ProductService(
            _dbContext,
            _productCreatedProducer.Object,
            _productUpdatedProducer.Object,
            _productDeletedProducer.Object,
            _stockUpdatedProducer.Object,
            _lowStockAlertProducer.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private Product CreateProduct(string? id = null, string? sku = null, string? name = null,
        string? categoryId = null, decimal price = 29.99m, int stockQuantity = 100,
        int minStockLevel = 10, bool isActive = true) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Sku = sku ?? $"SKU-{Guid.NewGuid():N}"[..20],
        Name = name ?? "Test Product",
        Description = "Test description",
        CategoryId = categoryId ?? "cat-1",
        Price = price,
        Cost = price * 0.6m,
        StockQuantity = stockQuantity,
        MinStockLevel = minStockLevel,
        MaxStockLevel = 1000,
        Unit = "pcs",
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private async Task<Product> SeedProductAsync(Product? product = null)
    {
        var p = product ?? CreateProduct();
        _dbContext.Products.Add(p);
        await _dbContext.SaveChangesAsync();
        return p;
    }

    // ==================== GetAllAsync ====================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllProducts()
    {
        await SeedProductAsync(CreateProduct(name: "Product A"));
        await SeedProductAsync(CreateProduct(name: "Product B"));

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnPagedResults()
    {
        for (int i = 0; i < 5; i++)
            await SeedProductAsync(CreateProduct(name: $"Product {i}"));

        var result = await _service.GetAllAsync(page: 1, pageSize: 2);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFilter_ShouldReturnOnlyActive()
    {
        await SeedProductAsync(CreateProduct(name: "Active", isActive: true));
        await SeedProductAsync(CreateProduct(name: "Inactive", isActive: false));

        var result = await _service.GetAllAsync(isActive: true);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetAllAsync_WithIsActiveFalseFilter_ShouldReturnOnlyInactive()
    {
        await SeedProductAsync(CreateProduct(name: "Active", isActive: true));
        await SeedProductAsync(CreateProduct(name: "Inactive", isActive: false));

        var result = await _service.GetAllAsync(isActive: false);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Inactive");
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    // ==================== GetTotalCountAsync ====================

    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnTotalCount()
    {
        await SeedProductAsync();
        await SeedProductAsync();
        await SeedProductAsync();

        var count = await _service.GetTotalCountAsync();

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithActiveFilter_ShouldCountOnlyActive()
    {
        await SeedProductAsync(CreateProduct(isActive: true));
        await SeedProductAsync(CreateProduct(isActive: false));

        var count = await _service.GetTotalCountAsync(isActive: true);

        count.Should().Be(1);
    }

    // ==================== GetByIdAsync ====================

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnProduct()
    {
        var product = await SeedProductAsync(CreateProduct(id: "prod-123", name: "Laptop"));

        var result = await _service.GetByIdAsync("prod-123");

        result.Should().NotBeNull();
        result!.Id.Should().Be("prod-123");
        result.Name.Should().Be("Laptop");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var result = await _service.GetByIdAsync("nonexistent");

        result.Should().BeNull();
    }

    // ==================== GetBySkuAsync ====================

    [Fact]
    public async Task GetBySkuAsync_WithExistingSku_ShouldReturnProduct()
    {
        await SeedProductAsync(CreateProduct(sku: "SKU-LAPTOP-001", name: "Laptop"));

        var result = await _service.GetBySkuAsync("SKU-LAPTOP-001");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Laptop");
    }

    [Fact]
    public async Task GetBySkuAsync_CaseInsensitive_ShouldReturnProduct()
    {
        await SeedProductAsync(CreateProduct(sku: "SKU-LAPTOP-001", name: "Laptop"));

        var result = await _service.GetBySkuAsync("sku-laptop-001");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Laptop");
    }

    [Fact]
    public async Task GetBySkuAsync_WithNonExistingSku_ShouldReturnNull()
    {
        var result = await _service.GetBySkuAsync("NONEXISTENT");

        result.Should().BeNull();
    }

    // ==================== GetLowStockProductsAsync ====================

    [Fact]
    public async Task GetLowStockProductsAsync_ShouldReturnLowStockProducts()
    {
        await SeedProductAsync(CreateProduct(name: "Low Stock", stockQuantity: 5, minStockLevel: 10, isActive: true));
        await SeedProductAsync(CreateProduct(name: "Normal Stock", stockQuantity: 100, minStockLevel: 10, isActive: true));

        var result = await _service.GetLowStockProductsAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Low Stock");
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ExcludesInactiveProducts()
    {
        await SeedProductAsync(CreateProduct(name: "Inactive Low", stockQuantity: 5, minStockLevel: 10, isActive: false));

        var result = await _service.GetLowStockProductsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLowStockProductsAsync_IncludesExactlyAtMinLevel()
    {
        await SeedProductAsync(CreateProduct(name: "At Min", stockQuantity: 10, minStockLevel: 10, isActive: true));

        var result = await _service.GetLowStockProductsAsync();

        result.Should().HaveCount(1);
    }

    // ==================== GetByCategoryIdAsync ====================

    [Fact]
    public async Task GetByCategoryIdAsync_ShouldReturnProductsInCategory()
    {
        await SeedProductAsync(CreateProduct(name: "A", categoryId: "cat-electronics"));
        await SeedProductAsync(CreateProduct(name: "B", categoryId: "cat-electronics"));
        await SeedProductAsync(CreateProduct(name: "C", categoryId: "cat-furniture"));

        var result = await _service.GetByCategoryIdAsync("cat-electronics");

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByCategoryIdAsync_WithNoProducts_ShouldReturnEmpty()
    {
        var result = await _service.GetByCategoryIdAsync("cat-empty");

        result.Should().BeEmpty();
    }

    // ==================== GetCountByCategoryIdAsync ====================

    [Fact]
    public async Task GetCountByCategoryIdAsync_ShouldReturnCorrectCount()
    {
        await SeedProductAsync(CreateProduct(categoryId: "cat-1"));
        await SeedProductAsync(CreateProduct(categoryId: "cat-1"));
        await SeedProductAsync(CreateProduct(categoryId: "cat-2"));

        var count = await _service.GetCountByCategoryIdAsync("cat-1");

        count.Should().Be(2);
    }

    // ==================== SearchAsync ====================

    [Fact]
    public async Task SearchAsync_ByName_ShouldReturnMatchingProducts()
    {
        await SeedProductAsync(CreateProduct(name: "Dell Laptop", sku: "DELL-001"));
        await SeedProductAsync(CreateProduct(name: "HP Laptop", sku: "HP-0001"));
        await SeedProductAsync(CreateProduct(name: "Samsung Monitor", sku: "SAM-001"));

        var result = await _service.SearchAsync("laptop");

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_BySku_ShouldReturnMatchingProducts()
    {
        await SeedProductAsync(CreateProduct(name: "Dell Laptop", sku: "DELL-001"));
        await SeedProductAsync(CreateProduct(name: "HP Laptop", sku: "HP-0001"));

        var result = await _service.SearchAsync("DELL");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Dell Laptop");
    }

    [Fact]
    public async Task SearchAsync_CaseInsensitive_ShouldWork()
    {
        await SeedProductAsync(CreateProduct(name: "UPPERCASE Product", sku: "UP-00001"));

        var result = await _service.SearchAsync("uppercase");

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_NoMatch_ShouldReturnEmpty()
    {
        await SeedProductAsync(CreateProduct(name: "Laptop", sku: "LAP-001"));

        var result = await _service.SearchAsync("nonexistent");

        result.Should().BeEmpty();
    }

    // ==================== CreateAsync ====================

    [Fact]
    public async Task CreateAsync_WithValidProduct_ShouldCreateProduct()
    {
        var product = CreateProduct(name: "New Product");

        var result = await _service.CreateAsync(product);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("New Product");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_ShouldPublishProductCreatedEvent()
    {
        var product = CreateProduct(name: "New Product", categoryId: "cat-1", price: 29.99m);

        await _service.CreateAsync(product);

        _productCreatedProducer.Verify(p => p.Produce(
            It.Is<DomainEvents.ProductCreated>(e =>
                e.Name == "New Product" &&
                e.CategoryId == "cat-1" &&
                e.Price == 29.99m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistProductInDatabase()
    {
        var product = CreateProduct(name: "Persisted Product");

        var result = await _service.CreateAsync(product);

        var fromDb = await _dbContext.Products.FindAsync(result.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Name.Should().Be("Persisted Product");
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignNewGuidId()
    {
        var product = CreateProduct();
        product.Id = "old-id";

        var result = await _service.CreateAsync(product);

        result.Id.Should().NotBe("old-id");
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    // ==================== UpdateAsync ====================

    [Fact]
    public async Task UpdateAsync_WithExistingProduct_ShouldUpdateAndReturnTrue()
    {
        await SeedProductAsync(CreateProduct(id: "prod-1", name: "Old Name"));

        var updated = CreateProduct(name: "New Name", price: 49.99m);
        var result = await _service.UpdateAsync("prod-1", updated);

        result.Should().BeTrue();
        var fromDb = await _dbContext.Products.FindAsync("prod-1");
        fromDb!.Name.Should().Be("New Name");
        fromDb.Price.Should().Be(49.99m);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTimestamp()
    {
        var oldTime = DateTime.UtcNow.AddDays(-1);
        var product = CreateProduct(id: "prod-1");
        product.UpdatedAt = oldTime;
        await SeedProductAsync(product);

        await _service.UpdateAsync("prod-1", CreateProduct());

        var fromDb = await _dbContext.Products.FindAsync("prod-1");
        fromDb!.UpdatedAt.Should().BeAfter(oldTime);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPublishProductUpdatedEvent()
    {
        await SeedProductAsync(CreateProduct(id: "prod-1"));

        await _service.UpdateAsync("prod-1", CreateProduct(name: "Updated", price: 99.99m));

        _productUpdatedProducer.Verify(p => p.Produce(
            It.Is<DomainEvents.ProductUpdated>(e =>
                e.ProductId == "prod-1" &&
                e.Name == "Updated" &&
                e.Price == 99.99m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingProduct_ShouldReturnFalse()
    {
        var result = await _service.UpdateAsync("nonexistent", CreateProduct());

        result.Should().BeFalse();
    }

    // ==================== UpdateStockAsync ====================

    [Fact]
    public async Task UpdateStockAsync_ShouldUpdateQuantity()
    {
        await SeedProductAsync(CreateProduct(id: "prod-1", stockQuantity: 100));

        var result = await _service.UpdateStockAsync("prod-1", 150, "user-1");

        result.Should().BeTrue();
        var fromDb = await _dbContext.Products.FindAsync("prod-1");
        fromDb!.StockQuantity.Should().Be(150);
    }

    [Fact]
    public async Task UpdateStockAsync_ShouldPublishStockUpdatedEvent()
    {
        await SeedProductAsync(CreateProduct(id: "prod-1", stockQuantity: 100));

        await _service.UpdateStockAsync("prod-1", 50, "user-1");

        _stockUpdatedProducer.Verify(p => p.Produce(
            It.Is<DomainEvents.StockUpdated>(e =>
                e.ProductId == "prod-1" &&
                e.NewQuantity == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStockAsync_WhenLowStock_ShouldPublishLowStockAlert()
    {
        await SeedProductAsync(CreateProduct(id: "prod-1", stockQuantity: 5, minStockLevel: 10));

        await _service.UpdateStockAsync("prod-1", 3, "user-1");

        _lowStockAlertProducer.Verify(p => p.Produce(
            It.Is<DomainEvents.LowStockAlert>(e => e.ProductId == "prod-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStockAsync_WithNonExistingProduct_ShouldReturnFalse()
    {
        var result = await _service.UpdateStockAsync("nonexistent", 100, "user-1");

        result.Should().BeFalse();
    }

    // ==================== DeleteAsync ====================

    [Fact]
    public async Task DeleteAsync_WithExistingProduct_ShouldDeleteAndReturnTrue()
    {
        await SeedProductAsync(CreateProduct(id: "prod-1"));

        var result = await _service.DeleteAsync("prod-1");

        result.Should().BeTrue();
        var fromDb = await _dbContext.Products.FindAsync("prod-1");
        fromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldPublishProductDeletedEvent()
    {
        await SeedProductAsync(CreateProduct(id: "prod-1"));

        await _service.DeleteAsync("prod-1");

        _productDeletedProducer.Verify(p => p.Produce(
            It.Is<DomainEvents.ProductDeleted>(e => e.ProductId == "prod-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingProduct_ShouldReturnFalse()
    {
        var result = await _service.DeleteAsync("nonexistent");

        result.Should().BeFalse();
    }

    // ==================== Model Tests ====================

    [Fact]
    public void Product_AvailableQuantity_ShouldCalculateCorrectly()
    {
        var product = new Product { StockQuantity = 100, ReservedQuantity = 30 };

        product.AvailableQuantity.Should().Be(70);
    }

    [Fact]
    public void Product_IsLowStock_WhenBelowMinLevel_ShouldBeTrue()
    {
        var product = new Product { StockQuantity = 5, ReservedQuantity = 0, MinStockLevel = 10 };

        product.IsLowStock.Should().BeTrue();
    }

    [Fact]
    public void Product_IsLowStock_WhenAboveMinLevel_ShouldBeFalse()
    {
        var product = new Product { StockQuantity = 100, ReservedQuantity = 0, MinStockLevel = 10 };

        product.IsLowStock.Should().BeFalse();
    }

    [Fact]
    public void Product_IsLowStock_ConsidersReservedQuantity()
    {
        var product = new Product { StockQuantity = 20, ReservedQuantity = 15, MinStockLevel = 10 };

        product.IsLowStock.Should().BeTrue();
    }

    [Fact]
    public void Product_DefaultValues_ShouldBeCorrect()
    {
        var product = new Product();

        product.MinStockLevel.Should().Be(10);
        product.MaxStockLevel.Should().Be(1000);
        product.Unit.Should().Be("pcs");
        product.IsActive.Should().BeTrue();
        product.StockQuantity.Should().Be(0);
        product.ReservedQuantity.Should().Be(0);
    }
}
