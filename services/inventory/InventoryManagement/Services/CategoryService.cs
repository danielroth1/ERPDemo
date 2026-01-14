using Microsoft.EntityFrameworkCore;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;

namespace InventoryManagement.Services;

public class CategoryService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(AppDbContext dbContext, ILogger<CategoryService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await _dbContext.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(string id)
    {
        return await _dbContext.Categories.FindAsync(id);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        category.Id = Guid.NewGuid().ToString();
        category.CreatedAt = DateTime.UtcNow;
        
        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Category created: {CategoryId} - {CategoryName}", category.Id, category.Name);

        return category;
    }

    public async Task<bool> UpdateAsync(string id, Category category)
    {
        var existingCategory = await _dbContext.Categories.FindAsync(id);
        if (existingCategory == null) return false;

        existingCategory.Name = category.Name;
        existingCategory.Description = category.Description;
        existingCategory.IsActive = category.IsActive;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Category updated: {CategoryId}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var category = await _dbContext.Categories.FindAsync(id);
        if (category == null) return false;

        _dbContext.Categories.Remove(category);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Category deleted: {CategoryId}", id);
        return true;
    }
}
