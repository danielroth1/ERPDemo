using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DashboardAnalytics.Services;
using DashboardAnalytics.Models.DTOs;

namespace DashboardAnalytics.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class KPIsController : ControllerBase
{
    private readonly IKPIService _kpiService;
    private readonly ILogger<KPIsController> _logger;

    public KPIsController(IKPIService kpiService, ILogger<KPIsController> logger)
    {
        _kpiService = kpiService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<KPIResponse>>> CreateKPI([FromBody] CreateKPIRequest request)
    {
        var kpi = await _kpiService.CreateKPIAsync(request);
        var response = new KPIResponse(
            kpi.Id,
            kpi.Name,
            kpi.Description,
            kpi.CurrentValue,
            kpi.TargetValue,
            kpi.PreviousValue,
            kpi.PercentageChange,
            kpi.Status.ToString(),
            kpi.LastUpdated
        );
        return CreatedAtAction(nameof(GetKPIById), new { id = kpi.Id }, ApiResponse<KPIResponse>.SuccessResponse(response, "KPI created successfully"));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<KPIResponse>>> GetKPIById(string id)
    {
        var kpi = await _kpiService.GetKPIByIdAsync(id);
        if (kpi == null)
            return NotFound(ApiResponse<KPIResponse>.ErrorResponse("KPI not found"));

        var response = new KPIResponse(
            kpi.Id,
            kpi.Name,
            kpi.Description,
            kpi.CurrentValue,
            kpi.TargetValue,
            kpi.PreviousValue,
            kpi.PercentageChange,
            kpi.Status.ToString(),
            kpi.LastUpdated
        );
        return Ok(ApiResponse<KPIResponse>.SuccessResponse(response));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<KPIResponse>>>> GetAllKPIs()
    {
        var kpis = await _kpiService.GetAllKPIsAsync();
        var response = kpis.Select(k => new KPIResponse(
            k.Id,
            k.Name,
            k.Description,
            k.CurrentValue,
            k.TargetValue,
            k.PreviousValue,
            k.PercentageChange,
            k.Status.ToString(),
            k.LastUpdated
        )).ToList();
        return Ok(ApiResponse<List<KPIResponse>>.SuccessResponse(response));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<KPIResponse>>> UpdateKPI(string id, [FromBody] UpdateKPIRequest request)
    {
        var kpi = await _kpiService.UpdateKPIAsync(id, request);
        if (kpi == null)
            return NotFound(ApiResponse<KPIResponse>.ErrorResponse("KPI not found"));

        var response = new KPIResponse(
            kpi.Id,
            kpi.Name,
            kpi.Description,
            kpi.CurrentValue,
            kpi.TargetValue,
            kpi.PreviousValue,
            kpi.PercentageChange,
            kpi.Status.ToString(),
            kpi.LastUpdated
        );
        return Ok(ApiResponse<KPIResponse>.SuccessResponse(response, "KPI updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteKPI(string id)
    {
        var result = await _kpiService.DeleteKPIAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.ErrorResponse("KPI not found"));

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "KPI deleted successfully"));
    }
}
