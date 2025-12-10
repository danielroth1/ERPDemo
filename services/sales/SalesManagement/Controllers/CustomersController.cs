using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Models.DTOs;
using SalesManagement.Services;

namespace SalesManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(ICustomerService customerService, ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "User,Manager,Admin")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            var customer = await _customerService.CreateCustomerAsync(request);
            return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, ApiResponse<CustomerResponse>.SuccessResponse(customer, "Customer created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CustomerResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetCustomerById(string id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            return NotFound(ApiResponse<CustomerResponse>.ErrorResponse("Customer not found"));
        }
        return Ok(ApiResponse<CustomerResponse>.SuccessResponse(customer));
    }

    /// <summary>
    /// Get customer by email
    /// </summary>
    [HttpGet("email/{email}")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> GetCustomerByEmail(string email)
    {
        var customer = await _customerService.GetCustomerByEmailAsync(email);
        if (customer == null)
        {
            return NotFound(ApiResponse<CustomerResponse>.ErrorResponse("Customer not found"));
        }
        return Ok(ApiResponse<CustomerResponse>.SuccessResponse(customer));
    }

    /// <summary>
    /// Get all customers with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CustomerResponse>>>> GetAllCustomers(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var customers = await _customerService.GetAllCustomersAsync(skip, limit);
        return Ok(ApiResponse<List<CustomerResponse>>.SuccessResponse(customers));
    }

    /// <summary>
    /// Search customers by name, email, or company
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<List<CustomerResponse>>>> SearchCustomers(
        [FromQuery] string q,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(ApiResponse<List<CustomerResponse>>.ErrorResponse("Search term is required"));
        }

        var customers = await _customerService.SearchCustomersAsync(q, skip, limit);
        return Ok(ApiResponse<List<CustomerResponse>>.SuccessResponse(customers));
    }

    /// <summary>
    /// Update an existing customer
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<CustomerResponse>>> UpdateCustomer(
        string id,
        [FromBody] UpdateCustomerRequest request)
    {
        try
        {
            var customer = await _customerService.UpdateCustomerAsync(id, request);
            if (customer == null)
            {
                return NotFound(ApiResponse<CustomerResponse>.ErrorResponse("Customer not found"));
            }
            return Ok(ApiResponse<CustomerResponse>.SuccessResponse(customer, "Customer updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CustomerResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Delete a customer (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCustomer(string id)
    {
        var deleted = await _customerService.DeleteCustomerAsync(id);
        if (!deleted)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Customer not found"));
        }
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Customer deleted successfully"));
    }
}
