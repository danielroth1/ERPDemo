using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MassTransit;
using ERP.Contracts.Commands;
using Orchestration.Services;

namespace Orchestration.Controllers;

[ApiController]
[Route("api/v1/shop")]
[Authorize]
public class ShopController : ControllerBase
{
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly PurchaseTracker _purchaseTracker;
    private readonly ReturnTracker _returnTracker;
    private readonly ILogger<ShopController> _logger;

    private static readonly TimeSpan SagaTimeout = TimeSpan.FromSeconds(30);

    public ShopController(
        ISendEndpointProvider sendEndpointProvider,
        PurchaseTracker purchaseTracker,
        ReturnTracker returnTracker,
        ILogger<ShopController> logger)
    {
        _sendEndpointProvider = sendEndpointProvider;
        _purchaseTracker = purchaseTracker;
        _returnTracker = returnTracker;
        _logger = logger;
    }

    [HttpPost("purchase/{productId}")]
    public async Task<IActionResult> PurchaseProduct(string productId, [FromQuery] int quantity = 1)
    {
        if (quantity < 1)
            return BadRequest(new { success = false, message = "Quantity must be at least 1" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var authToken = Request.Headers["Authorization"].ToString();
        var (correlationId, resultTask) = _purchaseTracker.CreatePending(SagaTimeout);

        _logger.LogInformation("Submitting purchase saga {CorrelationId} for product {ProductId}, quantity {Quantity}",
            correlationId, productId, quantity);

        var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:submit-purchase"));
        await endpoint.Send(new SubmitPurchase
        {
            CorrelationId = correlationId,
            UserId = userId,
            ProductId = productId,
            Quantity = quantity,
            AuthToken = authToken
        });

        try
        {
            var result = await resultTask;

            return Ok(new
            {
                success = true,
                message = "Purchase successful",
                data = new
                {
                    productId = result.ProductId,
                    productName = result.ProductName,
                    quantityPurchased = result.QuantityPurchased,
                    remainingStock = result.RemainingStock,
                    totalCost = result.TotalCost
                }
            });
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Purchase saga {CorrelationId} timed out", correlationId);
            return StatusCode(504, new { success = false, message = "Purchase timed out. Please try again." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Purchase saga {CorrelationId} failed: {Reason}", correlationId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("return/{productId}")]
    public async Task<IActionResult> ReturnProduct(string productId, [FromQuery] int quantity = 1)
    {
        if (quantity < 1)
            return BadRequest(new { success = false, message = "Quantity must be at least 1" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var authToken = Request.Headers["Authorization"].ToString();
        var (correlationId, resultTask) = _returnTracker.CreatePending(SagaTimeout);

        _logger.LogInformation("Submitting return saga {CorrelationId} for product {ProductId}, quantity {Quantity}",
            correlationId, productId, quantity);

        var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:submit-return"));
        await endpoint.Send(new SubmitReturn
        {
            CorrelationId = correlationId,
            UserId = userId,
            ProductId = productId,
            Quantity = quantity,
            AuthToken = authToken
        });

        try
        {
            var result = await resultTask;

            return Ok(new
            {
                success = true,
                message = "Return successful",
                data = new
                {
                    productId = result.ProductId,
                    productName = result.ProductName,
                    quantityReturned = result.QuantityReturned,
                    newStock = result.NewStock,
                    refundAmount = result.RefundAmount
                }
            });
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Return saga {CorrelationId} timed out", correlationId);
            return StatusCode(504, new { success = false, message = "Return timed out. Please try again." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Return saga {CorrelationId} failed: {Reason}", correlationId, ex.Message);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
