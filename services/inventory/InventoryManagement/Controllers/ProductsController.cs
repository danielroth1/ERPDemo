using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryManagement.Models;
using InventoryManagement.Models.DTOs;
using InventoryManagement.Services;

namespace InventoryManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ProductResponse>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(ApiResponse<PaginatedResponse<ProductResponse>>.ErrorResponse("Invalid pagination parameters"));
        }

        var products = await _productService.GetAllAsync(page, pageSize, isActive);
        var response = new List<ProductResponse>();

        foreach (var product in products)
        {
            response.Add(await _productService.MapToResponse(product));
        }

        var totalCount = await _productService.GetTotalCountAsync(isActive);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var paginatedResponse = new PaginatedResponse<ProductResponse>
        {
            Items = response,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        return Ok(ApiResponse<PaginatedResponse<ProductResponse>>.SuccessResponse(paginatedResponse));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetById(string id)
    {
        var product = await _productService.GetByIdAsync(id);

        if (product == null)
        {
            return NotFound(ApiResponse<ProductResponse>.ErrorResponse("Product not found"));
        }

        return Ok(ApiResponse<ProductResponse>.SuccessResponse(await _productService.MapToResponse(product)));
    }

    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetBySku(string sku)
    {
        var product = await _productService.GetBySkuAsync(sku);

        if (product == null)
        {
            return NotFound(ApiResponse<ProductResponse>.ErrorResponse("Product not found"));
        }

        return Ok(ApiResponse<ProductResponse>.SuccessResponse(await _productService.MapToResponse(product)));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<ProductResponse>>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(ApiResponse<List<ProductResponse>>.ErrorResponse("Search term is required"));
        }

        var products = await _productService.SearchAsync(q);
        var response = new List<ProductResponse>();

        foreach (var product in products)
        {
            response.Add(await _productService.MapToResponse(product));
        }

        return Ok(ApiResponse<List<ProductResponse>>.SuccessResponse(response));
    }

    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<ApiResponse<List<ProductResponse>>>> GetByCategory(string categoryId)
    {
        var products = await _productService.GetByCategoryIdAsync(categoryId);
        var response = new List<ProductResponse>();

        foreach (var product in products)
        {
            response.Add(await _productService.MapToResponse(product));
        }

        return Ok(ApiResponse<List<ProductResponse>>.SuccessResponse(response));
    }

    [HttpGet("category/{categoryId}/count")]
    public async Task<ActionResult<ApiResponse<int>>> GetCategoryProductCount(string categoryId)
    {
        var count = await _productService.GetCountByCategoryIdAsync(categoryId);
        return Ok(ApiResponse<int>.SuccessResponse(count));
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<List<LowStockAlert>>>> GetLowStock()
    {
        var products = await _productService.GetLowStockProductsAsync();

        var alerts = products.Select(p => new LowStockAlert
        {
            ProductId = p.Id,
            ProductName = p.Name,
            Sku = p.Sku,
            CurrentStock = p.StockQuantity,
            MinStockLevel = p.MinStockLevel,
            ReorderQuantity = p.MaxStockLevel - p.StockQuantity
        }).ToList();

        return Ok(ApiResponse<List<LowStockAlert>>.SuccessResponse(alerts));
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create([FromBody] ProductRequest request)
    {
        // Check if SKU already exists
        var existing = await _productService.GetBySkuAsync(request.Sku);
        if (existing != null)
        {
            return BadRequest(ApiResponse<ProductResponse>.ErrorResponse("Product with this SKU already exists"));
        }

        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Price = request.Price,
            Cost = request.Cost,
            StockQuantity = request.StockQuantity,
            MinStockLevel = request.MinStockLevel,
            MaxStockLevel = request.MaxStockLevel,
            Unit = request.Unit
        };

        await _productService.CreateAsync(product);

        var response = await _productService.MapToResponse(product);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ApiResponse<ProductResponse>.SuccessResponse(response, "Product created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(string id, [FromBody] ProductRequest request)
    {
        var existingProduct = await _productService.GetByIdAsync(id);

        if (existingProduct == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Product not found"));
        }

        existingProduct.Sku = request.Sku;
        existingProduct.Name = request.Name;
        existingProduct.Description = request.Description;
        existingProduct.CategoryId = request.CategoryId;
        existingProduct.Price = request.Price;
        existingProduct.Cost = request.Cost;
        existingProduct.StockQuantity = request.StockQuantity;
        existingProduct.MinStockLevel = request.MinStockLevel;
        existingProduct.MaxStockLevel = request.MaxStockLevel;
        existingProduct.Unit = request.Unit;

        var success = await _productService.UpdateAsync(id, existingProduct);

        if (success)
        {
            return Ok(ApiResponse<EmptyResponse>.SuccessResponse(new EmptyResponse(), "Product updated successfully"));
        }

        return StatusCode(500, ApiResponse<EmptyResponse>.ErrorResponse("Failed to update product"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<EmptyResponse>>> Delete(string id)
    {
        var success = await _productService.DeleteAsync(id);

        if (success)
        {
            return Ok(ApiResponse<EmptyResponse>.SuccessResponse(new EmptyResponse(), "Product deleted successfully"));
        }

        return NotFound(ApiResponse<EmptyResponse>.ErrorResponse("Product not found"));
    }

    [HttpPost("{id}/adjust-stock")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<StockAdjustmentResponse>>> AdjustStock(string id, [FromBody] StockAdjustmentRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";

        var success = await _productService.UpdateStockAsync(id, request.NewQuantity, userId);

        if (success)
        {
            var response = new StockAdjustmentResponse { NewQuantity = request.NewQuantity };
            return Ok(ApiResponse<StockAdjustmentResponse>.SuccessResponse(response, "Stock adjusted successfully"));
        }

        return NotFound(ApiResponse<StockAdjustmentResponse>.ErrorResponse("Product not found"));
    }

    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SeedProductsResponse>>> SeedProducts()
    {
        try
        {
            _logger.LogInformation("Starting product seeding process");

            // Delete all existing products (batch operation)
            var existingProducts = await _productService.GetAllAsync(1, int.MaxValue, null);
            var deleteCount = existingProducts.Count();
            
            // Delete in parallel for better performance
            var deleteTasks = existingProducts.Select(p => _productService.DeleteAsync(p.Id));
            await Task.WhenAll(deleteTasks);
            
            _logger.LogInformation("Deleted {Count} existing products", deleteCount);

            // Get seed data
            var seedProducts = SeedData.GetSampleProducts();
            var random = new Random();

            // Pre-create all categories in one batch
            var uniqueCategoryNames = seedProducts
                .Select(s => s.CategoryName)
                .Distinct()
                .ToList();

            var existingCategories = await _productService.GetAllCategoriesAsync();
            var categoryCache = new Dictionary<string, string>();

            foreach (var categoryName in uniqueCategoryNames)
            {
                var category = existingCategories.FirstOrDefault(c => c.Name == categoryName);
                if (category == null)
                {
                    var newCategory = await _productService.CreateCategoryAsync(new CreateCategoryRequest
                    {
                        Name = categoryName,
                        Description = $"{categoryName} products"
                    });
                    categoryCache[categoryName] = newCategory.Id;
                }
                else
                {
                    categoryCache[categoryName] = category.Id;
                }
            }

            _logger.LogInformation("Prepared {Count} categories", categoryCache.Count);

            // Create all products in parallel batches
            var createTasks = seedProducts.Select(async seed =>
            {
                var quantity = random.Next(1, 101);
                var createRequest = new CreateProductRequest
                {
                    Name = seed.Name,
                    Description = seed.Description,
                    CategoryId = categoryCache[seed.CategoryName],
                    Price = seed.Price,
                    Sku = seed.Sku,
                    Quantity = quantity
                };

                await _productService.CreateAsync(createRequest);
            });

            // Process in batches of 10 to avoid overwhelming the database
            var batchSize = 10;
            var batches = seedProducts
                .Select((product, index) => new { product, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.product).ToList());

            var createdCount = 0;
            foreach (var batch in batches)
            {
                var batchTasks = batch.Select(async seed =>
                {
                    var quantity = random.Next(1, 101);
                    var createRequest = new CreateProductRequest
                    {
                        Name = seed.Name,
                        Description = seed.Description,
                        CategoryId = categoryCache[seed.CategoryName],
                        Price = seed.Price,
                        Sku = seed.Sku,
                        Quantity = quantity
                    };

                    await _productService.CreateAsync(createRequest);
                });

                await Task.WhenAll(batchTasks);
                createdCount += batch.Count;
                _logger.LogInformation("Created batch of {Count} products (total: {Total})", batch.Count, createdCount);
            }

            _logger.LogInformation("Successfully seeded {Count} products", createdCount);

            var response = new SeedProductsResponse
            {
                ProductsCreated = createdCount,
                ProductsDeleted = deleteCount
            };

            return Ok(ApiResponse<SeedProductsResponse>.SuccessResponse(
                response,
                $"Successfully seeded {createdCount} products (deleted {deleteCount} existing products)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed products");
            return StatusCode(500, ApiResponse<SeedProductsResponse>.ErrorResponse($"Failed to seed products: {ex.Message}"));
        }
    }
}
