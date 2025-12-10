using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinancialManagement.Models.DTOs;
using FinancialManagement.Services;

namespace FinancialManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<ApiResponse<BalanceSheetResponse>>> GetBalanceSheet([FromQuery] DateTime? asOfDate)
    {
        var date = asOfDate ?? DateTime.UtcNow;
        var report = await _reportService.GenerateBalanceSheetAsync(date);
        return Ok(ApiResponse<BalanceSheetResponse>.SuccessResponse(report));
    }

    [HttpGet("income-statement")]
    public async Task<ActionResult<ApiResponse<IncomeStatementResponse>>> GetIncomeStatement(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        if (startDate >= endDate)
        {
            return BadRequest(ApiResponse<IncomeStatementResponse>.ErrorResponse("Start date must be before end date"));
        }

        var report = await _reportService.GenerateIncomeStatementAsync(startDate, endDate);
        return Ok(ApiResponse<IncomeStatementResponse>.SuccessResponse(report));
    }
}
