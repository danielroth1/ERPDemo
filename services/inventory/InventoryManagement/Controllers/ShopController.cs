using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryManagement.Models;
using InventoryManagement.Models.DTOs;
using InventoryManagement.Services;
using System.Security.Claims;

namespace InventoryManagement.Controllers;

/// <summary>
/// Shop controller for customer-facing product operations
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ShopController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly IFinancialServiceClient _financialClient;
    private readonly IFinancialAccountInitializer _accountInitializer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShopController> _logger;

    public ShopController(
        ProductService productService,
        CategoryService categoryService,
        IFinancialServiceClient financialClient,
        IFinancialAccountInitializer accountInitializer,
        IConfiguration configuration,
        ILogger<ShopController> logger)
    {
        _productService = productService;
        _categoryService = categoryService;
        _financialClient = financialClient;
        _accountInitializer = accountInitializer;
        _configuration = configuration;
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

    /// <summary>
    /// Purchase a product (reduces stock)
    /// </summary>
    [HttpPost("purchase/{productId}")]
    public async Task<ActionResult<ApiResponse<PurchaseResponse>>> PurchaseProduct(
        string productId,
        [FromQuery] int quantity = 1)
    {
        if (quantity < 1)
        {
            return BadRequest(ApiResponse<PurchaseResponse>.ErrorResponse("Quantity must be at least 1"));
        }

        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
        {
            return NotFound(ApiResponse<PurchaseResponse>.ErrorResponse("Product not found"));
        }

        if (!product.IsActive)
        {
            return BadRequest(ApiResponse<PurchaseResponse>.ErrorResponse("Product is not available"));
        }

        if (product.StockQuantity < quantity)
        {
            return BadRequest(ApiResponse<PurchaseResponse>.ErrorResponse($"Insufficient stock. Available: {product.StockQuantity}"));
        }

        // Get user ID from claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var totalCost = product.Price * quantity;

        // Step 1: Create financial transaction
        try
        {
            var transactionCreated = await CreateFinancialTransactionAsync(userId, productId, product.Name, quantity, totalCost);
            if (!transactionCreated)
            {
                return StatusCode(500, ApiResponse<PurchaseResponse>.ErrorResponse("Failed to create financial transaction"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create financial transaction for product {ProductId}", productId);
            return StatusCode(500, ApiResponse<PurchaseResponse>.ErrorResponse("Financial transaction failed. Purchase cancelled."));
        }

        // Step 2: Only if transaction successful, deduct stock
        product.StockQuantity -= quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _productService.UpdateAsync(productId, product);

        _logger.LogInformation(
            "Product purchased: {ProductName} (ID: {ProductId}), Quantity: {Quantity}, Remaining Stock: {Stock}",
            product.Name, productId, quantity, product.StockQuantity);

        var response = new PurchaseResponse
        {
            ProductId = productId,
            ProductName = product.Name,
            QuantityPurchased = quantity,
            RemainingStock = product.StockQuantity,
            TotalCost = totalCost
        };

        return Ok(ApiResponse<PurchaseResponse>.SuccessResponse(response, "Purchase successful"));
    }

    /// <summary>
    /// Create a financial transaction for the purchase
    /// </summary>
    private async Task<bool> CreateFinancialTransactionAsync(string userId, string productId, string productName, int quantity, decimal totalCost)
    {
        try
        {
            // Get JWT token from current request
            var token = Request.Headers["Authorization"].ToString();

            // First, get user account
            var userAccountId = await _financialClient.GetUserAccountIdAsync(userId, token);
            if (string.IsNullOrEmpty(userAccountId))
            {
                _logger.LogError("Failed to get user account for user {UserId}", userId);
                return false;
            }

            // Create transaction request with double-entry bookkeeping
            // Debit: User's account (asset decrease)
            // Credit: Revenue account (income increase)
            var revenueAccountId = await _accountInitializer.GetOrCreateRevenueAccountIdAsync();
            if (string.IsNullOrEmpty(revenueAccountId))
            {
                _logger.LogError("Failed to get revenue account ID");
                return false;
            }

            var transactionRequest = new CreateFinancialTransactionRequest
            {
                Description = $"Purchase of {productName} (Qty: {quantity})",
                Type = "Sale",
                ReferenceId = productId,
                ReferenceType = "Product",
                Entries = new List<FinancialJournalEntry>
                {
                    new FinancialJournalEntry
                    {
                        AccountId = userAccountId,
                        Debit = totalCost,
                        Credit = 0m,
                        Memo = $"Payment for {productName}"
                    },
                    new FinancialJournalEntry
                    {
                        AccountId = revenueAccountId,
                        Debit = 0m,
                        Credit = totalCost,
                        Memo = $"Sale of {productName}"
                    }
                }
            };

            var success = await _financialClient.CreateTransactionAsync(transactionRequest, token);
            if (success)
            {
                _logger.LogInformation("Financial transaction created successfully for product {ProductId}", productId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating financial transaction for product {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Return a product (increases stock)
    /// </summary>
    [HttpPost("return/{productId}")]
    public async Task<ActionResult<ApiResponse<ReturnResponse>>> ReturnProduct(
        string productId,
        [FromQuery] int quantity = 1)
    {
        if (quantity < 1)
        {
            return BadRequest(ApiResponse<ReturnResponse>.ErrorResponse("Quantity must be at least 1"));
        }

        var product = await _productService.GetByIdAsync(productId);
        if (product == null)
        {
            return NotFound(ApiResponse<ReturnResponse>.ErrorResponse("Product not found"));
        }

        // Get user ID from claims
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var refundAmount = product.Price * quantity;

        // Step 1: Create refund financial transaction (reverse of purchase)
        try
        {
            var transactionCreated = await CreateRefundTransactionAsync(userId, productId, product.Name, quantity, refundAmount);
            if (!transactionCreated)
            {
                return StatusCode(500, ApiResponse<ReturnResponse>.ErrorResponse("Failed to create refund transaction"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create refund transaction for product {ProductId}", productId);
            return StatusCode(500, ApiResponse<ReturnResponse>.ErrorResponse("Refund transaction failed. Return cancelled."));
        }

        // Step 2: Only if transaction successful, increase stock
        product.StockQuantity += quantity;
        product.UpdatedAt = DateTime.UtcNow;
        await _productService.UpdateAsync(productId, product);

        _logger.LogInformation(
            "Product returned: {ProductName} (ID: {ProductId}), Quantity: {Quantity}, New Stock: {Stock}",
            product.Name, productId, quantity, product.StockQuantity);

        var response = new ReturnResponse
        {
            ProductId = productId,
            ProductName = product.Name,
            QuantityReturned = quantity,
            NewStock = product.StockQuantity,
            RefundAmount = refundAmount
        };

        return Ok(ApiResponse<ReturnResponse>.SuccessResponse(response, "Return successful"));
    }

    /// <summary>
    /// Create a refund financial transaction for product return
    /// </summary>
    private async Task<bool> CreateRefundTransactionAsync(string userId, string productId, string productName, int quantity, decimal refundAmount)
    {
        try
        {
            // Get JWT token from current request
            var token = Request.Headers["Authorization"].ToString();

            // First, get user account
            var userAccountId = await _financialClient.GetUserAccountIdAsync(userId, token);
            if (string.IsNullOrEmpty(userAccountId))
            {
                _logger.LogError("Failed to get user account for user {UserId}", userId);
                return false;
            }

            // Create refund transaction with double-entry bookkeeping
            // Credit: User's account (asset increase - refund)
            // Debit: Revenue account (income decrease - reversal)
            var revenueAccountId = await _accountInitializer.GetOrCreateRevenueAccountIdAsync();
            if (string.IsNullOrEmpty(revenueAccountId))
            {
                _logger.LogError("Failed to get revenue account ID");
                return false;
            }

            var transactionRequest = new CreateFinancialTransactionRequest
            {
                Description = $"Refund for return of {productName} (Qty: {quantity})",
                Type = "Return",
                ReferenceId = productId,
                ReferenceType = "Product",
                Entries = new List<FinancialJournalEntry>
                {
                    new FinancialJournalEntry
                    {
                        AccountId = userAccountId,
                        Debit = 0m,
                        Credit = refundAmount,
                        Memo = $"Refund for {productName}"
                    },
                    new FinancialJournalEntry
                    {
                        AccountId = revenueAccountId,
                        Debit = refundAmount,
                        Credit = 0m,
                        Memo = $"Revenue reversal for {productName} return"
                    }
                }
            };

            var success = await _financialClient.CreateTransactionAsync(transactionRequest, token);
            if (success)
            {
                _logger.LogInformation("Refund transaction created successfully for product {ProductId}", productId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating refund transaction for product {ProductId}", productId);
            return false;
        }
    }
}
