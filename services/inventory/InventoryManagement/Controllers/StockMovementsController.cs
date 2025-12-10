using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryManagement.Models;
using InventoryManagement.Models.DTOs;
using InventoryManagement.Services;

namespace InventoryManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly StockMovementService _movementService;
    private readonly ProductService _productService;
    private readonly ILogger<StockMovementsController> _logger;

    public StockMovementsController(
        StockMovementService movementService,
        ProductService productService,
        ILogger<StockMovementsController> logger)
    {
        _movementService = movementService;
        _productService = productService;
        _logger = logger;
    }

    [HttpGet("product/{productId}")]
    public async Task<ActionResult<ApiResponse<List<StockMovementResponse>>>> GetByProduct(string productId, [FromQuery] int limit = 50)
    {
        var movements = await _movementService.GetByProductIdAsync(productId, limit);
        var response = new List<StockMovementResponse>();

        foreach (var movement in movements)
        {
            var product = await _productService.GetByIdAsync(movement.ProductId);

            response.Add(new StockMovementResponse
            {
                Id = movement.Id,
                ProductId = movement.ProductId,
                ProductName = product?.Name ?? "",
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                Reference = movement.Reference,
                Notes = movement.Notes,
                CreatedBy = movement.CreatedBy,
                CreatedAt = movement.CreatedAt
            });
        }

        return Ok(ApiResponse<List<StockMovementResponse>>.SuccessResponse(response));
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<List<StockMovementResponse>>>> GetRecent([FromQuery] int limit = 100)
    {
        var movements = await _movementService.GetRecentMovementsAsync(limit);
        var response = new List<StockMovementResponse>();

        foreach (var movement in movements)
        {
            var product = await _productService.GetByIdAsync(movement.ProductId);

            response.Add(new StockMovementResponse
            {
                Id = movement.Id,
                ProductId = movement.ProductId,
                ProductName = product?.Name ?? "",
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                Reference = movement.Reference,
                Notes = movement.Notes,
                CreatedBy = movement.CreatedBy,
                CreatedAt = movement.CreatedAt
            });
        }

        return Ok(ApiResponse<List<StockMovementResponse>>.SuccessResponse(response));
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<StockMovementResponse>>> Create([FromBody] StockMovementRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "system";

        var movement = new StockMovement
        {
            ProductId = request.ProductId,
            MovementType = request.MovementType,
            Quantity = request.Quantity,
            Reference = request.Reference,
            Notes = request.Notes
        };

        await _movementService.CreateAsync(movement, userId);

        var product = await _productService.GetByIdAsync(movement.ProductId);

        var response = new StockMovementResponse
        {
            Id = movement.Id,
            ProductId = movement.ProductId,
            ProductName = product?.Name ?? "",
            MovementType = movement.MovementType,
            Quantity = movement.Quantity,
            Reference = movement.Reference,
            Notes = movement.Notes,
            CreatedBy = movement.CreatedBy,
            CreatedAt = movement.CreatedAt
        };

        return CreatedAtAction(nameof(GetByProduct), new { productId = movement.ProductId }, ApiResponse<StockMovementResponse>.SuccessResponse(response, "Stock movement created successfully"));
    }
}
