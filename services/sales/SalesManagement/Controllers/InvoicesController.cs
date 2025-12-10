using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SalesManagement.Models;
using SalesManagement.Models.DTOs;
using SalesManagement.Services;

namespace SalesManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(IInvoiceService invoiceService, ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new invoice from an order
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        try
        {
            var invoice = await _invoiceService.CreateInvoiceAsync(request);
            return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, ApiResponse<InvoiceResponse>.SuccessResponse(invoice, "Invoice created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<InvoiceResponse>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InvoiceResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Get invoice by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetInvoiceById(string id)
    {
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
        if (invoice == null)
        {
            return NotFound(ApiResponse<InvoiceResponse>.ErrorResponse("Invoice not found"));
        }
        return Ok(ApiResponse<InvoiceResponse>.SuccessResponse(invoice));
    }

    /// <summary>
    /// Get invoice by order ID
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetInvoiceByOrderId(string orderId)
    {
        var invoice = await _invoiceService.GetInvoiceByOrderIdAsync(orderId);
        if (invoice == null)
        {
            return NotFound(ApiResponse<InvoiceResponse>.ErrorResponse("Invoice not found for this order"));
        }
        return Ok(ApiResponse<InvoiceResponse>.SuccessResponse(invoice));
    }

    /// <summary>
    /// Get all invoices with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InvoiceResponse>>>> GetAllInvoices(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var invoices = await _invoiceService.GetAllInvoicesAsync(skip, limit);
        return Ok(ApiResponse<List<InvoiceResponse>>.SuccessResponse(invoices));
    }

    /// <summary>
    /// Get invoices by customer ID
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<ApiResponse<List<InvoiceResponse>>>> GetInvoicesByCustomer(
        string customerId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var invoices = await _invoiceService.GetInvoicesByCustomerAsync(customerId, skip, limit);
        return Ok(ApiResponse<List<InvoiceResponse>>.SuccessResponse(invoices));
    }

    /// <summary>
    /// Get invoices by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<ApiResponse<List<InvoiceResponse>>>> GetInvoicesByStatus(
        string status,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        if (!Enum.TryParse<InvoiceStatus>(status, true, out var invoiceStatus))
        {
            return BadRequest(ApiResponse<List<InvoiceResponse>>.ErrorResponse("Invalid invoice status"));
        }

        var invoices = await _invoiceService.GetInvoicesByStatusAsync(invoiceStatus, skip, limit);
        return Ok(ApiResponse<List<InvoiceResponse>>.SuccessResponse(invoices));
    }

    /// <summary>
    /// Get overdue invoices
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<ApiResponse<List<InvoiceResponse>>>> GetOverdueInvoices(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var invoices = await _invoiceService.GetOverdueInvoicesAsync(skip, limit);
        return Ok(ApiResponse<List<InvoiceResponse>>.SuccessResponse(invoices));
    }

    /// <summary>
    /// Update an existing invoice
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> UpdateInvoice(
        string id,
        [FromBody] UpdateInvoiceRequest request)
    {
        try
        {
            var invoice = await _invoiceService.UpdateInvoiceAsync(id, request);
            if (invoice == null)
            {
                return NotFound(ApiResponse<InvoiceResponse>.ErrorResponse("Invoice not found"));
            }
            return Ok(ApiResponse<InvoiceResponse>.SuccessResponse(invoice, "Invoice updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InvoiceResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Record a payment for an invoice
    /// </summary>
    [HttpPost("{id}/payments")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> RecordPayment(
        string id,
        [FromBody] RecordPaymentRequest request)
    {
        try
        {
            var invoice = await _invoiceService.RecordPaymentAsync(id, request);
            if (invoice == null)
            {
                return NotFound(ApiResponse<InvoiceResponse>.ErrorResponse("Invoice not found"));
            }
            return Ok(ApiResponse<InvoiceResponse>.SuccessResponse(invoice, "Payment recorded successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<InvoiceResponse>.ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<InvoiceResponse>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Delete an invoice (draft only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteInvoice(string id)
    {
        try
        {
            var deleted = await _invoiceService.DeleteInvoiceAsync(id);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Invoice not found"));
            }
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "Invoice deleted successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }
}
