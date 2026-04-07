using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;
using InventoryManagement.Services;
using InventoryManagement.Tests.Helpers;

namespace InventoryManagement.Tests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<CategoryService>> _loggerMock;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _loggerMock = new Mock<ILogger<CategoryService>>();
        _service = new CategoryService(_dbContext, _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private Category CreateCategory(string? id = null, string? name = null,
        string? description = null, bool isActive = true) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Name = name ?? $"Category-{Guid.NewGuid():N}"[..20],
        Description = description ?? "Test description",
        IsActive = isActive,
        CreatedAt = DateTime.UtcNow
    };

    private async Task<Category> SeedCategoryAsync(Category? category = null)
    {
        var c = category ?? CreateCategory();
        _dbContext.Categories.Add(c);
        await _dbContext.SaveChangesAsync();
        return c;
    }

    // ==================== GetAllAsync ====================

    [Fact]
    public async Task GetAllAsync_ShouldReturnOnlyActiveCategories()
    {
        await SeedCategoryAsync(CreateCategory(name: "Active", isActive: true));
        await SeedCategoryAsync(CreateCategory(name: "Inactive", isActive: false));

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnOrderedByName()
    {
        await SeedCategoryAsync(CreateCategory(name: "Zebra"));
        await SeedCategoryAsync(CreateCategory(name: "Apple"));
        await SeedCategoryAsync(CreateCategory(name: "Mango"));

        var result = await _service.GetAllAsync();

        result.Select(c => c.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    // ==================== GetByIdAsync ====================

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnCategory()
    {
        var category = await SeedCategoryAsync(CreateCategory(id: "cat-1", name: "Electronics"));

        var result = await _service.GetByIdAsync("cat-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("cat-1");
        result.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var result = await _service.GetByIdAsync("nonexistent");

        result.Should().BeNull();
    }

    // ==================== CreateAsync ====================

    [Fact]
    public async Task CreateAsync_WithValidCategory_ShouldCreateCategory()
    {
        var category = CreateCategory(name: "New Category");

        var result = await _service.CreateAsync(category);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Name.Should().Be("New Category");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignNewGuidId()
    {
        var category = CreateCategory();
        category.Id = "old-id";

        var result = await _service.CreateAsync(category);

        result.Id.Should().NotBe("old-id");
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistInDatabase()
    {
        var category = CreateCategory(name: "Persisted");

        var result = await _service.CreateAsync(category);

        var fromDb = await _dbContext.Categories.FindAsync(result.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Name.Should().Be("Persisted");
    }

    // ==================== UpdateAsync ====================

    [Fact]
    public async Task UpdateAsync_WithExistingCategory_ShouldUpdateAndReturnTrue()
    {
        await SeedCategoryAsync(CreateCategory(id: "cat-1", name: "Old Name"));

        var updated = CreateCategory(name: "New Name", description: "Updated desc");
        var result = await _service.UpdateAsync("cat-1", updated);

        result.Should().BeTrue();
        var fromDb = await _dbContext.Categories.FindAsync("cat-1");
        fromDb!.Name.Should().Be("New Name");
        fromDb.Description.Should().Be("Updated desc");
    }

    [Fact]
    public async Task UpdateAsync_CanDeactivateCategory()
    {
        await SeedCategoryAsync(CreateCategory(id: "cat-1", isActive: true));

        var updated = CreateCategory(isActive: false);
        await _service.UpdateAsync("cat-1", updated);

        var fromDb = await _dbContext.Categories.FindAsync("cat-1");
        fromDb!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingCategory_ShouldReturnFalse()
    {
        var result = await _service.UpdateAsync("nonexistent", CreateCategory());

        result.Should().BeFalse();
    }

    // ==================== DeleteAsync ====================

    [Fact]
    public async Task DeleteAsync_WithExistingCategory_ShouldDeleteAndReturnTrue()
    {
        await SeedCategoryAsync(CreateCategory(id: "cat-1"));

        var result = await _service.DeleteAsync("cat-1");

        result.Should().BeTrue();
        var fromDb = await _dbContext.Categories.FindAsync("cat-1");
        fromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingCategory_ShouldReturnFalse()
    {
        var result = await _service.DeleteAsync("nonexistent");

        result.Should().BeFalse();
    }

    // ==================== Model Tests ====================

    [Fact]
    public void Category_DefaultValues_ShouldBeCorrect()
    {
        var category = new Category();

        category.Id.Should().BeEmpty();
        category.Name.Should().BeEmpty();
        category.Description.Should().BeEmpty();
        category.IsActive.Should().BeTrue();
    }
}
