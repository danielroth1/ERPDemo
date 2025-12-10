using FluentAssertions;
using Moq;
using InventoryManagement.Models;
using InventoryManagement.Repositories;
using InventoryManagement.Services;
using Xunit;

namespace InventoryManagement.Tests.Services;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IStockMovementRepository> _stockMovementRepositoryMock;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _productRepositoryMock = new Mock<IProductRepository>();
        _stockMovementRepositoryMock = new Mock<IStockMovementRepository>();
        _productService = new ProductService(
            _productRepositoryMock.Object,
            _stockMovementRepositoryMock.Object
        );
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = "1", Name = "Product 1", SKU = "SKU001", UnitPrice = 10.99m, StockQuantity = 100 },
            new() { Id = "2", Name = "Product 2", SKU = "SKU002", UnitPrice = 20.99m, StockQuantity = 50 }
        };

        _productRepositoryMock
            .Setup(r => r.GetAllAsync(1, 10))
            .ReturnsAsync((products, products.Count));

        // Act
        var (result, total) = await _productService.GetAllAsync(1, 10);

        // Assert
        result.Should().HaveCount(2);
        total.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnProduct()
    {
        // Arrange
        var productId = "123";
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            SKU = "SKU001",
            UnitPrice = 10.99m,
            StockQuantity = 100
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task CreateAsync_WithValidProduct_ShouldCreateProduct()
    {
        // Arrange
        var product = new Product
        {
            Name = "New Product",
            SKU = "SKU001",
            Description = "Test description",
            CategoryId = "cat-1",
            UnitPrice = 29.99m,
            StockQuantity = 100,
            ReorderLevel = 20,
            IsActive = true
        };

        _productRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => { p.Id = "new-id"; return p; });

        // Act
        var result = await _productService.CreateAsync(product);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("New Product");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateProduct()
    {
        // Arrange
        var productId = "123";
        var existingProduct = new Product
        {
            Id = productId,
            Name = "Old Name",
            SKU = "SKU001",
            UnitPrice = 10.99m,
            StockQuantity = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var updatedProduct = new Product
        {
            Id = productId,
            Name = "New Name",
            SKU = "SKU001",
            UnitPrice = 15.99m,
            StockQuantity = 150
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(existingProduct);

        _productRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        // Act
        var result = await _productService.UpdateAsync(productId, updatedProduct);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.UnitPrice.Should().Be(15.99m);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var productId = "123";
        
        _productRepositoryMock
            .Setup(r => r.DeleteAsync(productId))
            .ReturnsAsync(true);

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetLowStockProductsAsync_ShouldReturnProductsBelowReorderLevel()
    {
        // Arrange
        var lowStockProducts = new List<Product>
        {
            new() { Id = "1", Name = "Product 1", StockQuantity = 5, ReorderLevel = 10 },
            new() { Id = "2", Name = "Product 2", StockQuantity = 8, ReorderLevel = 15 }
        };

        _productRepositoryMock
            .Setup(r => r.GetLowStockProductsAsync())
            .ReturnsAsync(lowStockProducts);

        // Act
        var result = await _productService.GetLowStockProductsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.StockQuantity <= p.ReorderLevel).Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnMatchingProducts()
    {
        // Arrange
        var searchQuery = "laptop";
        var matchingProducts = new List<Product>
        {
            new() { Id = "1", Name = "Laptop Dell", SKU = "LAP001" },
            new() { Id = "2", Name = "Laptop HP", SKU = "LAP002" }
        };

        _productRepositoryMock
            .Setup(r => r.SearchAsync(searchQuery))
            .ReturnsAsync(matchingProducts);

        // Act
        var result = await _productService.SearchAsync(searchQuery);

        // Assert
        result.Should().HaveCount(2);
        result.All(p => p.Name.Contains("Laptop", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }

    [Fact]
    public async Task AdjustStockAsync_WithPositiveQuantity_ShouldIncreaseStock()
    {
        // Arrange
        var productId = "123";
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            SKU = "SKU001",
            StockQuantity = 100
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        _stockMovementRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<StockMovement>()))
            .ReturnsAsync((StockMovement sm) => sm);

        // Act
        var result = await _productService.AdjustStockAsync(productId, 50, "Adjustment", "Stock adjustment");

        // Assert
        result.Should().NotBeNull();
        result!.StockQuantity.Should().Be(150);
        
        _stockMovementRepositoryMock.Verify(r => r.CreateAsync(It.Is<StockMovement>(sm =>
            sm.ProductId == productId &&
            sm.Quantity == 50 &&
            sm.Type == "Adjustment"
        )), Times.Once);
    }

    [Fact]
    public async Task AdjustStockAsync_WithNegativeQuantity_ShouldDecreaseStock()
    {
        // Arrange
        var productId = "123";
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            SKU = "SKU001",
            StockQuantity = 100
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _productRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product p) => p);

        _stockMovementRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<StockMovement>()))
            .ReturnsAsync((StockMovement sm) => sm);

        // Act
        var result = await _productService.AdjustStockAsync(productId, -30, "Sale", "Product sold");

        // Assert
        result.Should().NotBeNull();
        result!.StockQuantity.Should().Be(70);
    }

    [Fact]
    public async Task AdjustStockAsync_WithInsufficientStock_ShouldThrowException()
    {
        // Arrange
        var productId = "123";
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            SKU = "SKU001",
            StockQuantity = 10
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _productService.AdjustStockAsync(productId, -50, "Sale", "Product sold")
        );
    }
}
