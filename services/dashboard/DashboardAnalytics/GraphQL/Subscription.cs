using DashboardAnalytics.Models.DTOs;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;

namespace DashboardAnalytics.GraphQL;

public class Subscription
{
    [Subscribe]
    [Topic("dashboardUpdate")]
    public DashboardMetricsResponse OnDashboardUpdate(
        [EventMessage] DashboardMetricsResponse metrics)
    {
        return metrics;
    }

    [Subscribe]
    [Topic("alertReceived")]
    public AlertResponse OnAlertReceived(
        [EventMessage] AlertResponse alert)
    {
        return alert;
    }

    [Subscribe]
    [Topic("kpiUpdated")]
    public KPIResponse OnKPIUpdated(
        [EventMessage] KPIResponse kpi)
    {
        return kpi;
    }
}
