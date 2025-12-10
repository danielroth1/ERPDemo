using DashboardAnalytics.Models.DTOs;
using DashboardAnalytics.Services;

namespace DashboardAnalytics.GraphQL;

public class Mutation
{
    public async Task<KPIResponse> CreateKPI(
        CreateKPIRequest request,
        [Service] IKPIService kpiService)
    {
        var kpi = await kpiService.CreateKPIAsync(request);
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

    public async Task<KPIResponse?> UpdateKPI(
        string id,
        UpdateKPIRequest request,
        [Service] IKPIService kpiService)
    {
        var kpi = await kpiService.UpdateKPIAsync(id, request);
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

    public async Task<bool> DeleteKPI(
        string id,
        [Service] IKPIService kpiService)
    {
        return await kpiService.DeleteKPIAsync(id);
    }

    public async Task<bool> MarkAlertAsRead(
        string id,
        [Service] IAlertService alertService)
    {
        return await alertService.MarkAsReadAsync(id);
    }

    public async Task<bool> DeleteAlert(
        string id,
        [Service] IAlertService alertService)
    {
        return await alertService.DeleteAlertAsync(id);
    }
}
