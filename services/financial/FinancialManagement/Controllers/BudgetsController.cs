using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Services;

namespace FinancialManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;
    private readonly ILogger<BudgetsController> _logger;

    public BudgetsController(IBudgetService budgetService, ILogger<BudgetsController> logger)
    {
        _budgetService = budgetService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<BudgetResponse>>> CreateBudget([FromBody] CreateBudgetRequest request)
    {
        try
        {
            var budget = await _budgetService.CreateBudgetAsync(request);
            return CreatedAtAction(nameof(GetBudgetById), new { id = budget.Id }, ApiResponse<BudgetResponse>.SuccessResponse(budget, "Budget created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BudgetResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<BudgetResponse>>> GetBudgetById(string id)
    {
        var budget = await _budgetService.GetBudgetByIdAsync(id);
        if (budget == null)
        {
            return NotFound(ApiResponse<BudgetResponse>.ErrorResponse("Budget not found"));
        }
        return Ok(ApiResponse<BudgetResponse>.SuccessResponse(budget));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BudgetResponse>>>> GetAllBudgets(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var budgets = await _budgetService.GetAllBudgetsAsync(skip, limit);
        return Ok(ApiResponse<List<BudgetResponse>>.SuccessResponse(budgets));
    }

    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<List<BudgetResponse>>>> GetActiveBudgets(
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var budgets = await _budgetService.GetActiveBudgetsAsync(skip, limit);
        return Ok(ApiResponse<List<BudgetResponse>>.SuccessResponse(budgets));
    }

    [HttpGet("account/{accountId}")]
    public async Task<ActionResult<ApiResponse<List<BudgetResponse>>>> GetBudgetsByAccount(
        string accountId,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 100)
    {
        var budgets = await _budgetService.GetBudgetsByAccountAsync(accountId, skip, limit);
        return Ok(ApiResponse<List<BudgetResponse>>.SuccessResponse(budgets));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<ApiResponse<BudgetResponse>>> UpdateBudget(
        string id,
        [FromBody] UpdateBudgetRequest request)
    {
        var budget = await _budgetService.UpdateBudgetAsync(id, request);
        if (budget == null)
        {
            return NotFound(ApiResponse<BudgetResponse>.ErrorResponse("Budget not found"));
        }
        return Ok(ApiResponse<BudgetResponse>.SuccessResponse(budget, "Budget updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBudget(string id)
    {
        var deleted = await _budgetService.DeleteBudgetAsync(id);
        if (!deleted)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Budget not found"));
        }
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Budget deleted successfully"));
    }
}
