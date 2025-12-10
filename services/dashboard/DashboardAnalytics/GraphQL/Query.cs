using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;

namespace DashboardAnalytics.GraphQL;

public class Query
{
    public async Task<DashboardMetricsResponse> GetDashboardMetrics(
        [Service] IAnalyticsService analyticsService)
    {
        return await analyticsService.GetDashboardMetricsAsync();
    }

    public async Task<SalesOverviewResponse> GetSalesOverview(
        [Service] IAnalyticsService analyticsService)
    {
        return await analyticsService.GetSalesOverviewAsync();
    }

    public async Task<InventoryOverviewResponse> GetInventoryOverview(
        [Service] IAnalyticsService analyticsService)
    {
        return await analyticsService.GetInventoryOverviewAsync();
    }

    public async Task<FinancialSummaryResponse> GetFinancialSummary(
        [Service] IAnalyticsService analyticsService)
    {
        return await analyticsService.GetFinancialSummaryAsync();
    }

    public async Task<List<KPIResponse>> GetAllKPIs(
        [Service] IKPIService kpiService)
    {
        var kpis = await kpiService.GetAllKPIsAsync();
        return kpis.Select(k => new KPIResponse(
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
    }

    public async Task<KPIResponse?> GetKPIById(
        string id,
        [Service] IKPIService kpiService)
    {
        var kpi = await kpiService.GetKPIByIdAsync(id);
        if (kpi == null) return null;

        return new KPIResponse(
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
    }

    public async Task<List<AlertResponse>> GetAllAlerts(
        int page,
        int pageSize,
        [Service] IAlertService alertService)
    {
        var alerts = await alertService.GetAllAlertsAsync(page, pageSize);
        return alerts.Select(a => new AlertResponse(
            a.Id,
            a.Title,
            a.Message,
            a.Severity.ToString(),
            a.Source,
            a.Data,
            a.IsRead,
            a.CreatedAt
        )).ToList();
    }

    public async Task<List<AlertResponse>> GetUnreadAlerts(
        [Service] IAlertService alertService)
    {
        var alerts = await alertService.GetUnreadAlertsAsync();
        return alerts.Select(a => new AlertResponse(
            a.Id,
            a.Title,
            a.Message,
            a.Severity.ToString(),
            a.Source,
            a.Data,
            a.IsRead,
            a.CreatedAt
        )).ToList();
    }
}
