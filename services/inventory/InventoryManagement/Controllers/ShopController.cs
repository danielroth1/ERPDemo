using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryManagement.Models;
using InventoryManagement.Models.DTOs;
using InventoryManagement.Services;

namespace InventoryManagement.Controllers;

/// <summary>
/// Shop controller for customer-facing product browsing.
/// Purchase and return operations are handled by the Gateway via MassTransit sagas.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ShopController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly ILogger<ShopController> _logger;

    public ShopController(
        ProductService productService,
        CategoryService categoryService,
        ILogger<ShopController> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available products for shopping
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<List<ProductResponse>>>> GetAvailableProducts(
        [FromQuery] string? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(ApiResponse<List<ProductResponse>>.ErrorResponse("Invalid pagination parameters"));
        }

        var products = await _productService.GetAllAsync(page, pageSize, isActive: true);
        var response = new List<ProductResponse>();

        foreach (var product in products)
        {
            // Filter by category if specified
            if (!string.IsNullOrEmpty(categoryId) && product.CategoryId != categoryId)
                continue;

            // Only include products with stock
            if (product.StockQuantity > 0)
            {
                response.Add(await _productService.MapToResponse(product));
            }
        }

        return Ok(ApiResponse<List<ProductResponse>>.SuccessResponse(response));
    }

    /// <summary>
    /// Get product categories for filtering
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetCategories()
    {
        var categories = await _categoryService.GetAllAsync();
        var response = categories
            .Where(c => c.IsActive)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToList();

        return Ok(ApiResponse<List<CategoryResponse>>.SuccessResponse(response));
    }
}
