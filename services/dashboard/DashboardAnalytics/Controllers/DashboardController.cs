using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DashboardAnalytics.Services;
using DashboardAnalytics.Models.DTOs;

namespace DashboardAnalytics.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IAnalyticsService analyticsService,
        ILogger<DashboardController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<ApiResponse<DashboardMetricsResponse>>> GetDashboardMetrics()
    {
        var metrics = await _analyticsService.GetDashboardMetricsAsync();
        return Ok(ApiResponse<DashboardMetricsResponse>.SuccessResponse(metrics));
    }

    [HttpGet("sales")]
    public async Task<ActionResult<ApiResponse<SalesOverviewResponse>>> GetSalesOverview()
    {
        var overview = await _analyticsService.GetSalesOverviewAsync();
        return Ok(ApiResponse<SalesOverviewResponse>.SuccessResponse(overview));
    }

    [HttpGet("inventory")]
    public async Task<ActionResult<ApiResponse<InventoryOverviewResponse>>> GetInventoryOverview()
    {
        var overview = await _analyticsService.GetInventoryOverviewAsync();
        return Ok(ApiResponse<InventoryOverviewResponse>.SuccessResponse(overview));
    }

    [HttpGet("financial")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<FinancialSummaryResponse>>> GetFinancialSummary()
    {
        var summary = await _analyticsService.GetFinancialSummaryAsync();
        return Ok(ApiResponse<FinancialSummaryResponse>.SuccessResponse(summary));
    }
}
