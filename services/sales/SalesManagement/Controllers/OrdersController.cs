using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Models;
using SalesManagement.Models.DTOs;
using SalesManagement.Services;

namespace SalesManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "User,Manager,Admin")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(request);
            return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, ApiResponse<OrderResponse>.SuccessResponse(order, "Order created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<OrderResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> GetOrderById(string id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound(ApiResponse<OrderResponse>.ErrorResponse("Order not found"));
        }
        return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
    }

    /// <summary>
    /// Get all orders with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OrderResponse>>>> GetAllOrders(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var orders = await _orderService.GetAllOrdersAsync(skip, limit);
        return Ok(ApiResponse<List<OrderResponse>>.SuccessResponse(orders));
    }

    /// <summary>
    /// Get orders by customer ID
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<ApiResponse<List<OrderResponse>>>> GetOrdersByCustomer(
        string customerId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var orders = await _orderService.GetOrdersByCustomerAsync(customerId, skip, limit);
        return Ok(ApiResponse<List<OrderResponse>>.SuccessResponse(orders));
    }

    /// <summary>
    /// Get orders by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<ApiResponse<List<OrderResponse>>>> GetOrdersByStatus(
        string status,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
        {
            return BadRequest(ApiResponse<List<OrderResponse>>.ErrorResponse("Invalid order status"));
        }

        var orders = await _orderService.GetOrdersByStatusAsync(orderStatus, skip, limit);
        return Ok(ApiResponse<List<OrderResponse>>.SuccessResponse(orders));
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> UpdateOrder(
        string id,
        [FromBody] UpdateOrderRequest request)
    {
        try
        {
            var order = await _orderService.UpdateOrderAsync(id, request);
            if (order == null)
            {
                return NotFound(ApiResponse<OrderResponse>.ErrorResponse("Order not found"));
            }
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<OrderResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> UpdateOrderStatus(
        string id,
        [FromBody] UpdateOrderStatusRequest request)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
        {
            return BadRequest(ApiResponse<OrderResponse>.ErrorResponse("Invalid order status"));
        }

        var order = await _orderService.UpdateOrderStatusAsync(id, status);
        if (order == null)
        {
            return NotFound(ApiResponse<OrderResponse>.ErrorResponse("Order not found"));
        }
        return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order status updated successfully"));
    }

    /// <summary>
    /// Delete an order (draft only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteOrder(string id)
    {
        try
        {
            var deleted = await _orderService.DeleteOrderAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Order not found"));
            }
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Order deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }
}
