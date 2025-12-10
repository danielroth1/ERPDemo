using FluentAssertions;
using Moq;
using InventoryManagement.Models;
using InventoryManagement.Repositories;
using InventoryManagement.Services;
using Xunit;

namespace InventoryManagement.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _categoryRepositoryMock = new Mock<ICategoryRepository>();
        _categoryService = new CategoryService(_categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = "1", Name = "Electronics", Description = "Electronic items" },
            new() { Id = "2", Name = "Furniture", Description = "Furniture items" }
        };

        _categoryRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _categoryService.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnCategory()
    {
        // Arrange
        var categoryId = "123";
        var category = new Category
        {
            Id = categoryId,
            Name = "Electronics",
            Description = "Electronic items"
        };

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var result = await _categoryService.GetByIdAsync(categoryId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(categoryId);
        result.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task CreateAsync_WithValidCategory_ShouldCreateCategory()
    {
        // Arrange
        var category = new Category
        {
            Name = "New Category",
            Description = "Test description"
        };

        _categoryRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => { c.Id = "new-id"; return c; });

        // Act
        var result = await _categoryService.CreateAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("New Category");
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateCategory()
    {
        // Arrange
        var categoryId = "123";
        var existingCategory = new Category
        {
            Id = categoryId,
            Name = "Old Name",
            Description = "Old description"
        };

        var updatedCategory = new Category
        {
            Id = categoryId,
            Name = "New Name",
            Description = "New description"
        };

        _categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(categoryId))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Category>()))
            .ReturnsAsync((Category c) => c);

        // Act
        var result = await _categoryService.UpdateAsync(categoryId, updatedCategory);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("New Name");
        result.Description.Should().Be("New description");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingId_ShouldReturnTrue()
    {
        // Arrange
        var categoryId = "123";
        
        _categoryRepositoryMock
            .Setup(r => r.DeleteAsync(categoryId))
            .ReturnsAsync(true);

        // Act
        var result = await _categoryService.DeleteAsync(categoryId);

        // Assert
        result.Should().BeTrue();
    }
}
