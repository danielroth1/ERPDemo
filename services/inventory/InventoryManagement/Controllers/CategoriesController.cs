using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryManagement.Models;
using InventoryManagement.Models.DTOs;
using InventoryManagement.Services;

namespace InventoryManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(CategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();

        var response = categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        }).ToList();

        return Ok(ApiResponse<List<CategoryResponse>>.SuccessResponse(response));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(string id)
    {
        var category = await _categoryService.GetByIdAsync(id);

        if (category == null)
        {
            return NotFound(ApiResponse<CategoryResponse>.ErrorResponse("Category not found"));
        }

        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        return Ok(ApiResponse<CategoryResponse>.SuccessResponse(response));
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create([FromBody] CategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description
        };

        await _categoryService.CreateAsync(category);

        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, ApiResponse<CategoryResponse>.SuccessResponse(response, "Category created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(string id, [FromBody] CategoryRequest request)
    {
        var existingCategory = await _categoryService.GetByIdAsync(id);

        if (existingCategory == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Category not found"));
        }

        existingCategory.Name = request.Name;
        existingCategory.Description = request.Description;

        var success = await _categoryService.UpdateAsync(id, existingCategory);

        if (success)
        {
            return Ok(ApiResponse<EmptyResponse>.SuccessResponse(new EmptyResponse(), "Category updated successfully"));
        }

        return StatusCode(500, ApiResponse<EmptyResponse>.ErrorResponse("Failed to update category"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<EmptyResponse>>> Delete(string id)
    {
        var success = await _categoryService.DeleteAsync(id);

        if (success)
        {
            return Ok(ApiResponse<EmptyResponse>.SuccessResponse(new EmptyResponse(), "Category deleted successfully"));
        }

        return NotFound(ApiResponse<EmptyResponse>.ErrorResponse("Category not found"));
    }
}
