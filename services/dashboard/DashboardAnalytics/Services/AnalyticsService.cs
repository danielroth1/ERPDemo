using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
using DashboardAnalytics.Hubs;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;

namespace DashboardAnalytics.Services;

public interface IAnalyticsService
{
    Task ProcessUserEventAsync(UserEventDTO userEvent);
    Task ProcessProductEventAsync(ProductEventDTO productEvent);
    Task ProcessOrderEventAsync(OrderEventDTO orderEvent);
    Task ProcessTransactionEventAsync(TransactionEventDTO transactionEvent);
    Task ProcessLowStockAlertAsync(ProductEventDTO productEvent);
    Task ProcessBudgetExceededAlertAsync(BudgetEventDTO budgetEvent);
    Task<DashboardMetricsResponse> GetDashboardMetricsAsync();
    Task<SalesOverviewResponse> GetSalesOverviewAsync();
    Task<InventoryOverviewResponse> GetInventoryOverviewAsync();
    Task<FinancialSummaryResponse> GetFinancialSummaryAsync();
}

public class AnalyticsService : IAnalyticsService
{
    private readonly MongoDbContext _context;
    private readonly IHubContext<DashboardHub> _hubContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyticsService> _logger;

    // Aggregated counters (in-memory for real-time updates)
    private int _totalUsers = 0;
    private int _totalProducts = 0;
    private int _totalOrders = 0;
    private int _lowStockProducts = 0;
    private decimal _totalRevenue = 0;
    private decimal _totalExpenses = 0;

    public AnalyticsService(
        MongoDbContext context,
        IHubContext<DashboardHub> hubContext,
        IMemoryCache cache,
        ILogger<AnalyticsService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task ProcessUserEventAsync(UserEventDTO userEvent)
    {
        _logger.LogInformation("Processing user event: {EventType} for user {UserId}", 
            userEvent.EventType, userEvent.UserId);

        if (userEvent.EventType == "Created")
        {
            _totalUsers++;
            await RecordMetricAsync(MetricType.UserCount, _totalUsers);
        }

        await NotifyDashboardUpdate("user", new { userEvent.EventType, userEvent.UserId, userEvent.Email });
    }

    public async Task ProcessProductEventAsync(ProductEventDTO productEvent)
    {
        _logger.LogInformation("Processing product event: {EventType} for product {ProductId}", 
            productEvent.EventType, productEvent.ProductId);

        if (productEvent.EventType == "Created")
        {
            _totalProducts++;
            await RecordMetricAsync(MetricType.ProductCount, _totalProducts);
        }

        await NotifyDashboardUpdate("product", new { productEvent.EventType, productEvent.ProductId, productEvent.Name });
    }

    public async Task ProcessOrderEventAsync(OrderEventDTO orderEvent)
    {
        _logger.LogInformation("Processing order event: {EventType} for order {OrderId}", 
            orderEvent.EventType, orderEvent.OrderId);

        if (orderEvent.EventType == "Created")
        {
            _totalOrders++;
            _totalRevenue += orderEvent.TotalAmount;
            
            await RecordMetricAsync(MetricType.OrderCount, _totalOrders);
            await RecordMetricAsync(MetricType.Revenue, _totalRevenue);
        }

        await NotifyDashboardUpdate("order", new 
        { 
            orderEvent.EventType, 
            orderEvent.OrderId, 
            orderEvent.TotalAmount, 
            orderEvent.Status 
        });

        // Invalidate cached metrics
        _cache.Remove("dashboard_metrics");
        _cache.Remove("sales_overview");
    }

    public async Task ProcessTransactionEventAsync(TransactionEventDTO transactionEvent)
    {
        _logger.LogInformation("Processing transaction event: {Type} - {TransactionId}", 
            transactionEvent.Type, transactionEvent.TransactionId);

        // Update revenue or expenses based on transaction type
        if (transactionEvent.Type == "Sale" || transactionEvent.Type == "Revenue")
        {
            _totalRevenue += transactionEvent.TotalAmount;
            await RecordMetricAsync(MetricType.Revenue, _totalRevenue);
        }
        else if (transactionEvent.Type == "Expense" || transactionEvent.Type == "Payment")
        {
            _totalExpenses += transactionEvent.TotalAmount;
            await RecordMetricAsync(MetricType.Expenses, _totalExpenses);
        }

        await RecordMetricAsync(MetricType.NetIncome, _totalRevenue - _totalExpenses);

        await NotifyDashboardUpdate("transaction", new 
        { 
            transactionEvent.TransactionId, 
            transactionEvent.Type, 
            transactionEvent.TotalAmount 
        });

        // Invalidate cached metrics
        _cache.Remove("dashboard_metrics");
        _cache.Remove("financial_summary");
    }

    public async Task ProcessLowStockAlertAsync(ProductEventDTO productEvent)
    {
        _logger.LogWarning("Low stock alert for product {ProductId}: {Name}", 
            productEvent.ProductId, productEvent.Name);

        _lowStockProducts++;
        await RecordMetricAsync(MetricType.LowStockProducts, _lowStockProducts);

        var alert = new Alert
        {
            Title = "Low Stock Alert",
            Message = $"Product '{productEvent.Name}' is running low on stock. Current level: {productEvent.StockLevel}",
            Severity = AlertSeverity.Warning,
            Source = "Inventory",
            Data = new Dictionary<string, object>
            {
                ["productId"] = productEvent.ProductId,
                ["productName"] = productEvent.Name,
                ["stockLevel"] = productEvent.StockLevel
            }
        };

        await _context.Alerts.InsertOneAsync(alert);
        await _hubContext.Clients.All.SendAsync("ReceiveAlert", alert);

        _cache.Remove("inventory_overview");
    }

    public async Task ProcessBudgetExceededAlertAsync(BudgetEventDTO budgetEvent)
    {
        _logger.LogWarning("Budget exceeded alert for {BudgetName}: {ExceededAmount}", 
            budgetEvent.BudgetName, budgetEvent.ExceededAmount);

        var alert = new Alert
        {
            Title = "Budget Exceeded",
            Message = $"Budget '{budgetEvent.BudgetName}' has been exceeded by {budgetEvent.ExceededAmount:C}. " +
                     $"Current usage: {budgetEvent.PercentageUsed:F1}%",
            Severity = AlertSeverity.Error,
            Source = "Financial",
            Data = new Dictionary<string, object>
            {
                ["budgetId"] = budgetEvent.BudgetId,
                ["budgetName"] = budgetEvent.BudgetName,
                ["budgetAmount"] = budgetEvent.BudgetAmount,
                ["spentAmount"] = budgetEvent.SpentAmount,
                ["exceededAmount"] = budgetEvent.ExceededAmount,
                ["percentageUsed"] = budgetEvent.PercentageUsed
            }
        };

        await _context.Alerts.InsertOneAsync(alert);
        await _hubContext.Clients.All.SendAsync("ReceiveAlert", alert);
    }

    public async Task<DashboardMetricsResponse> GetDashboardMetricsAsync()
    {
        return await _cache.GetOrCreateAsync("dashboard_metrics", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            // Get latest metrics from database
            var userCountMetric = await GetLatestMetricAsync(MetricType.UserCount);
            var productCountMetric = await GetLatestMetricAsync(MetricType.ProductCount);
            var orderCountMetric = await GetLatestMetricAsync(MetricType.OrderCount);
            var revenueMetric = await GetLatestMetricAsync(MetricType.Revenue);
            var expensesMetric = await GetLatestMetricAsync(MetricType.Expenses);
            var netIncomeMetric = await GetLatestMetricAsync(MetricType.NetIncome);
            var lowStockMetric = await GetLatestMetricAsync(MetricType.LowStockProducts);

            return new DashboardMetricsResponse(
                TotalRevenue: revenueMetric?.Value ?? _totalRevenue,
                TotalExpenses: expensesMetric?.Value ?? _totalExpenses,
                NetIncome: netIncomeMetric?.Value ?? (_totalRevenue - _totalExpenses),
                TotalOrders: (int)(orderCountMetric?.Value ?? _totalOrders),
                TotalCustomers: 0, // Will be aggregated from sales service
                TotalProducts: (int)(productCountMetric?.Value ?? _totalProducts),
                LowStockProducts: (int)(lowStockMetric?.Value ?? _lowStockProducts),
                ActiveUsers: (int)(userCountMetric?.Value ?? _totalUsers),
                InventoryValue: 0, // Will be calculated from inventory
                LastUpdated: DateTime.UtcNow
            );
        }) ?? new DashboardMetricsResponse(0, 0, 0, 0, 0, 0, 0, 0, 0, DateTime.UtcNow);
    }

    public async Task<SalesOverviewResponse> GetSalesOverviewAsync()
    {
        return await _cache.GetOrCreateAsync("sales_overview", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // Get order metrics
            var ordersToday = await _context.Metrics
                .CountDocumentsAsync(m => m.Type == MetricType.OrderCount && m.Timestamp >= today);

            var ordersThisWeek = await _context.Metrics
                .CountDocumentsAsync(m => m.Type == MetricType.OrderCount && m.Timestamp >= weekStart);

            var ordersThisMonth = await _context.Metrics
                .CountDocumentsAsync(m => m.Type == MetricType.OrderCount && m.Timestamp >= monthStart);

            var averageOrderValue = _totalOrders > 0 ? _totalRevenue / _totalOrders : 0;

            return new SalesOverviewResponse(
                TotalOrders: _totalOrders,
                TotalRevenue: _totalRevenue,
                AverageOrderValue: averageOrderValue,
                OrdersToday: (int)ordersToday,
                OrdersThisWeek: (int)ordersThisWeek,
                OrdersThisMonth: (int)ordersThisMonth,
                TopProducts: new List<TopProductResponse>() // Would need product sales aggregation
            );
        }) ?? new SalesOverviewResponse(0, 0, 0, 0, 0, 0, new List<TopProductResponse>());
    }

    public async Task<InventoryOverviewResponse> GetInventoryOverviewAsync()
    {
        return await _cache.GetOrCreateAsync("inventory_overview", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            return new InventoryOverviewResponse(
                TotalProducts: _totalProducts,
                LowStockProducts: _lowStockProducts,
                OutOfStockProducts: 0, // Would need stock level aggregation
                TotalInventoryValue: 0, // Would need price * quantity aggregation
                LowStockItems: new List<LowStockProductResponse>()
            );
        }) ?? new InventoryOverviewResponse(0, 0, 0, 0, new List<LowStockProductResponse>());
    }

    public async Task<FinancialSummaryResponse> GetFinancialSummaryAsync()
    {
        return await _cache.GetOrCreateAsync("financial_summary", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            var profitMargin = _totalRevenue > 0 
                ? ((_totalRevenue - _totalExpenses) / _totalRevenue) * 100 
                : 0;

            return new FinancialSummaryResponse(
                TotalRevenue: _totalRevenue,
                TotalExpenses: _totalExpenses,
                NetIncome: _totalRevenue - _totalExpenses,
                ProfitMargin: profitMargin,
                ExpensesByCategory: new List<ExpenseByCategoryResponse>(),
                RevenueByMonth: new List<RevenueByMonthResponse>()
            );
        }) ?? new FinancialSummaryResponse(0, 0, 0, 0, new List<ExpenseByCategoryResponse>(), new List<RevenueByMonthResponse>());
    }

    private async Task RecordMetricAsync(MetricType type, decimal value)
    {
        var metric = new DashboardMetrics
        {
            Type = type,
            Label = type.ToString(),
            Value = value
        };

        await _context.Metrics.InsertOneAsync(metric);
    }

    private async Task<DashboardMetrics?> GetLatestMetricAsync(MetricType type)
    {
        return await _context.Metrics
            .Find(m => m.Type == type)
            .SortByDescending(m => m.Timestamp)
            .FirstOrDefaultAsync();
    }

    private async Task NotifyDashboardUpdate(string eventType, object data)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveDashboardUpdate", new
        {
            EventType = eventType,
            Data = data,
            Timestamp = DateTime.UtcNow
        });
    }
}
