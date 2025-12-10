using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DashboardAnalytics.Services;
using DashboardAnalytics.Models.DTOs;

namespace DashboardAnalytics.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AlertResponse>>> GetAlertById(string id)
    {
        var alert = await _alertService.GetAlertByIdAsync(id);
        if (alert == null)
            return NotFound(ApiResponse<AlertResponse>.ErrorResponse("Alert not found"));

        var response = new AlertResponse(
            alert.Id,
            alert.Title,
            alert.Message,
            alert.Severity.ToString(),
            alert.Source,
            alert.Data,
            alert.IsRead,
            alert.CreatedAt
        );
        return Ok(ApiResponse<AlertResponse>.SuccessResponse(response));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AlertResponse>>>> GetAllAlerts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var alerts = await _alertService.GetAllAlertsAsync(page, pageSize);
        var response = alerts.Select(a => new AlertResponse(
            a.Id,
            a.Title,
            a.Message,
            a.Severity.ToString(),
            a.Source,
            a.Data,
            a.IsRead,
            a.CreatedAt
        )).ToList();
        return Ok(ApiResponse<List<AlertResponse>>.SuccessResponse(response));
    }

    [HttpGet("unread")]
    public async Task<ActionResult<ApiResponse<List<AlertResponse>>>> GetUnreadAlerts()
    {
        var alerts = await _alertService.GetUnreadAlertsAsync();
        var response = alerts.Select(a => new AlertResponse(
            a.Id,
            a.Title,
            a.Message,
            a.Severity.ToString(),
            a.Source,
            a.Data,
            a.IsRead,
            a.CreatedAt
        )).ToList();
        return Ok(ApiResponse<List<AlertResponse>>.SuccessResponse(response));
    }

    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(string id)
    {
        var result = await _alertService.MarkAsReadAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.ErrorResponse("Alert not found"));

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Alert marked as read"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAlert(string id)
    {
        var result = await _alertService.DeleteAlertAsync(id);
        if (!result)
            return NotFound(ApiResponse<object>.ErrorResponse("Alert not found"));

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Alert deleted successfully"));
    }
}
