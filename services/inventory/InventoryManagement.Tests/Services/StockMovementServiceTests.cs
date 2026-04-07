using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;
using InventoryManagement.Services;
using InventoryManagement.Tests.Helpers;
using ERP.Contracts.Events.Domain;
using DomainEvents = ERP.Contracts.Events.Domain;

namespace InventoryManagement.Tests.Services;

public class StockMovementServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITopicProducer<StockMovementCreated>> _movementCreatedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.ProductCreated>> _productCreatedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.ProductUpdated>> _productUpdatedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.ProductDeleted>> _productDeletedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.StockUpdated>> _stockUpdatedProducer;
    private readonly Mock<ITopicProducer<DomainEvents.LowStockAlert>> _lowStockAlertProducer;
    private readonly ProductService _productService;
    private readonly StockMovementService _service;

    public StockMovementServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _movementCreatedProducer = new Mock<ITopicProducer<StockMovementCreated>>();
        _productCreatedProducer = new Mock<ITopicProducer<DomainEvents.ProductCreated>>();
        _productUpdatedProducer = new Mock<ITopicProducer<DomainEvents.ProductUpdated>>();
        _productDeletedProducer = new Mock<ITopicProducer<DomainEvents.ProductDeleted>>();
        _stockUpdatedProducer = new Mock<ITopicProducer<DomainEvents.StockUpdated>>();
        _lowStockAlertProducer = new Mock<ITopicProducer<DomainEvents.LowStockAlert>>();

        _productService = new ProductService(
            _dbContext,
            _productCreatedProducer.Object,
            _productUpdatedProducer.Object,
            _productDeletedProducer.Object,
            _stockUpdatedProducer.Object,
            _lowStockAlertProducer.Object,
            new Mock<ILogger<ProductService>>().Object);

        _service = new StockMovementService(
            _dbContext,
            _movementCreatedProducer.Object,
            _productService,
            new Mock<ILogger<StockMovementService>>().Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private async Task<Product> SeedProductAsync(string id = "prod-1", int stockQuantity = 100)
    {
        var product = new Product
        {
            Id = id,
            Sku = $"SKU-{Guid.NewGuid():N}"[..20],
            Name = "Test Product",
            Description = "Test",
            CategoryId = "cat-1",
            Price = 29.99m,
            Cost = 15.00m,
            StockQuantity = stockQuantity,
            MinStockLevel = 10,
            MaxStockLevel = 1000,
            Unit = "pcs",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();
        return product;
    }

    private StockMovement CreateMovement(string productId = "prod-1",
        MovementType type = MovementType.Purchase, int quantity = 50) => new()
    {
        ProductId = productId,
        MovementType = type,
        Quantity = quantity,
        Reference = "REF-001",
        Notes = "Test movement"
    };

    // ==================== GetByProductIdAsync ====================

    [Fact]
    public async Task GetByProductIdAsync_ShouldReturnMovementsForProduct()
    {
        var product = await SeedProductAsync();
        var movement = CreateMovement(product.Id);
        movement.Id = Guid.NewGuid().ToString();
        movement.CreatedBy = "user-1";
        movement.CreatedAt = DateTime.UtcNow;
        _dbContext.StockMovements.Add(movement);
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetByProductIdAsync(product.Id);

        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task GetByProductIdAsync_ShouldReturnOrderedByCreatedAtDescending()
    {
        var product = await SeedProductAsync();
        for (int i = 0; i < 3; i++)
        {
            var m = CreateMovement(product.Id, quantity: i + 10);
            m.Id = Guid.NewGuid().ToString();
            m.CreatedBy = "user-1";
            m.CreatedAt = DateTime.UtcNow.AddMinutes(-i);
            _dbContext.StockMovements.Add(m);
        }
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetByProductIdAsync(product.Id);

        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(m => m.CreatedAt);
    }

    [Fact]
    public async Task GetByProductIdAsync_WithLimit_ShouldRespectLimit()
    {
        var product = await SeedProductAsync();
        for (int i = 0; i < 10; i++)
        {
            var m = CreateMovement(product.Id);
            m.Id = Guid.NewGuid().ToString();
            m.CreatedBy = "user-1";
            m.CreatedAt = DateTime.UtcNow.AddMinutes(-i);
            _dbContext.StockMovements.Add(m);
        }
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetByProductIdAsync(product.Id, limit: 5);

        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetByProductIdAsync_EmptyResult_ShouldReturnEmptyList()
    {
        var result = await _service.GetByProductIdAsync("nonexistent");

        result.Should().BeEmpty();
    }

    // ==================== GetRecentMovementsAsync ====================

    [Fact]
    public async Task GetRecentMovementsAsync_ShouldReturnRecentMovements()
    {
        var product = await SeedProductAsync();
        for (int i = 0; i < 3; i++)
        {
            var m = CreateMovement(product.Id);
            m.Id = Guid.NewGuid().ToString();
            m.CreatedBy = "user-1";
            m.CreatedAt = DateTime.UtcNow.AddMinutes(-i);
            _dbContext.StockMovements.Add(m);
        }
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetRecentMovementsAsync();

        result.Should().HaveCount(3);
        result.Should().BeInDescendingOrder(m => m.CreatedAt);
    }

    [Fact]
    public async Task GetRecentMovementsAsync_WithLimit_ShouldRespectLimit()
    {
        var product = await SeedProductAsync();
        for (int i = 0; i < 10; i++)
        {
            var m = CreateMovement(product.Id);
            m.Id = Guid.NewGuid().ToString();
            m.CreatedBy = "user-1";
            m.CreatedAt = DateTime.UtcNow.AddMinutes(-i);
            _dbContext.StockMovements.Add(m);
        }
        await _dbContext.SaveChangesAsync();

        var result = await _service.GetRecentMovementsAsync(limit: 5);

        result.Should().HaveCount(5);
    }

    // ==================== CreateAsync ====================

    [Fact]
    public async Task CreateAsync_Purchase_ShouldIncreaseStock()
    {
        var product = await SeedProductAsync(stockQuantity: 100);

        var movement = CreateMovement(product.Id, MovementType.Purchase, 50);
        await _service.CreateAsync(movement, "user-1");

        var updatedProduct = await _dbContext.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().Be(150);
    }

    [Fact]
    public async Task CreateAsync_Sale_ShouldDecreaseStock()
    {
        var product = await SeedProductAsync(stockQuantity: 100);

        var movement = CreateMovement(product.Id, MovementType.Sale, 30);
        await _service.CreateAsync(movement, "user-1");

        var updatedProduct = await _dbContext.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().Be(70);
    }

    [Fact]
    public async Task CreateAsync_Return_ShouldIncreaseStock()
    {
        var product = await SeedProductAsync(stockQuantity: 100);

        var movement = CreateMovement(product.Id, MovementType.Return, 20);
        await _service.CreateAsync(movement, "user-1");

        var updatedProduct = await _dbContext.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().Be(120);
    }

    [Fact]
    public async Task CreateAsync_Adjustment_ShouldSetExactQuantity()
    {
        var product = await SeedProductAsync(stockQuantity: 100);

        var movement = CreateMovement(product.Id, MovementType.Adjustment, 75);
        await _service.CreateAsync(movement, "user-1");

        var updatedProduct = await _dbContext.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().Be(75);
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignNewId()
    {
        var product = await SeedProductAsync();
        var movement = CreateMovement(product.Id);

        var result = await _service.CreateAsync(movement, "user-1");

        result.Id.Should().NotBeNullOrEmpty();
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldSetCreatedBy()
    {
        var product = await SeedProductAsync();
        var movement = CreateMovement(product.Id);

        var result = await _service.CreateAsync(movement, "user-123");

        result.CreatedBy.Should().Be("user-123");
    }

    [Fact]
    public async Task CreateAsync_ShouldSetCreatedAt()
    {
        var product = await SeedProductAsync();
        var movement = CreateMovement(product.Id);

        var result = await _service.CreateAsync(movement, "user-1");

        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_ShouldPublishStockMovementCreatedEvent()
    {
        var product = await SeedProductAsync();
        var movement = CreateMovement(product.Id, MovementType.Purchase, 50);

        await _service.CreateAsync(movement, "user-1");

        _movementCreatedProducer.Verify(p => p.Produce(
            It.Is<StockMovementCreated>(e =>
                e.ProductId == product.Id &&
                e.Type == "Purchase" &&
                e.Quantity == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistMovementInDatabase()
    {
        var product = await SeedProductAsync();
        var movement = CreateMovement(product.Id);

        var result = await _service.CreateAsync(movement, "user-1");

        var fromDb = await _dbContext.StockMovements.FindAsync(result.Id);
        fromDb.Should().NotBeNull();
        fromDb!.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistingProduct_ShouldCreateMovementWithoutStockUpdate()
    {
        var movement = CreateMovement("nonexistent");

        var result = await _service.CreateAsync(movement, "user-1");

        result.Should().NotBeNull();
        result.ProductId.Should().Be("nonexistent");
    }

    // ==================== Model Tests ====================

    [Fact]
    public void StockMovement_DefaultValues_ShouldBeCorrect()
    {
        var movement = new StockMovement();

        movement.Id.Should().BeEmpty();
        movement.ProductId.Should().BeEmpty();
        movement.Reference.Should().BeEmpty();
        movement.Notes.Should().BeEmpty();
        movement.CreatedBy.Should().BeEmpty();
    }

    [Fact]
    public void MovementType_AllValues_ShouldBeValid()
    {
        var values = Enum.GetValues<MovementType>();

        values.Should().HaveCount(5);
        values.Should().Contain(new[]
        {
            MovementType.Purchase,
            MovementType.Sale,
            MovementType.Return,
            MovementType.Adjustment,
            MovementType.Transfer
        });
    }
}
