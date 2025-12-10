using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Services;
using System.Security.Claims;

namespace FinancialManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionService transactionService, ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        try
        {
            var createdBy = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
            var transaction = await _transactionService.CreateTransactionAsync(request, createdBy);
            return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, ApiResponse<TransactionResponse>.SuccessResponse(transaction, "Transaction created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> GetTransactionById(string id)
    {
        var transaction = await _transactionService.GetTransactionByIdAsync(id);
        if (transaction == null)
        {
            return NotFound(ApiResponse<TransactionResponse>.ErrorResponse("Transaction not found"));
        }
        return Ok(ApiResponse<TransactionResponse>.SuccessResponse(transaction));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<TransactionResponse>>>> GetAllTransactions(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var transactions = await _transactionService.GetAllTransactionsAsync(skip, limit);
        return Ok(ApiResponse<List<TransactionResponse>>.SuccessResponse(transactions));
    }

    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<ApiResponse<List<TransactionResponse>>>> GetTransactionsByAccount(
        string accountId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var transactions = await _transactionService.GetTransactionsByAccountAsync(accountId, skip, limit);
        return Ok(ApiResponse<List<TransactionResponse>>.SuccessResponse(transactions));
    }

    [HttpGet("date-range")]
    public async Task<ActionResult<ApiResponse<List<TransactionResponse>>>> GetTransactionsByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var transactions = await _transactionService.GetTransactionsByDateRangeAsync(startDate, endDate, skip, limit);
        return Ok(ApiResponse<List<TransactionResponse>>.SuccessResponse(transactions));
    }

    [HttpPost("{id}/void")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> VoidTransaction(string id)
    {
        try
        {
            var transaction = await _transactionService.VoidTransactionAsync(id);
            if (transaction == null)
            {
                return NotFound(ApiResponse<TransactionResponse>.ErrorResponse("Transaction not found"));
            }
            return Ok(ApiResponse<TransactionResponse>.SuccessResponse(transaction, "Transaction voided successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
        }
    }
}
