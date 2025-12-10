using MongoDB.Driver;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;

namespace InventoryManagement.Services;

public class CategoryService
{
    private readonly IMongoCollection<Category> _categories;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(MongoDbContext dbContext, ILogger<CategoryService> logger)
    {
        _categories = dbContext.GetCollection<Category>("categories");
        _logger = logger;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _categories
            .Find(c => c.IsActive)
            .SortBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(string id)
    {
        return await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Category> CreateAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        await _categories.InsertOneAsync(category);

        _logger.LogInformation("Category created: {CategoryId} - {CategoryName}", category.Id, category.Name);

        return category;
    }

    public async Task<bool> UpdateAsync(string id, Category category)
    {
        var result = await _categories.ReplaceOneAsync(c => c.Id == id, category);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Category updated: {CategoryId}", id);
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _categories.DeleteOneAsync(c => c.Id == id);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("Category deleted: {CategoryId}", id);
            return true;
        }

        return false;
    }
}
