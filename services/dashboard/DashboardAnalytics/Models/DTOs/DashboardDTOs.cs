namespace DashboardAnalytics.Models.DTOs;

// Request DTOs
public record CreateKPIRequest(
    string Name,
    string Description,
    decimal TargetValue
);

public record UpdateKPIRequest(
    decimal CurrentValue,
    decimal? TargetValue
);

// Response DTOs
public record DashboardMetricsResponse(
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal NetIncome,
    int TotalOrders,
    int TotalCustomers,
    int TotalProducts,
    int LowStockProducts,
    int ActiveUsers,
    decimal InventoryValue,
    DateTime LastUpdated
);

public record KPIResponse(
    string Id,
    string Name,
    string Description,
    decimal CurrentValue,
    decimal TargetValue,
    decimal PreviousValue,
    decimal PercentageChange,
    string Status,
    DateTime LastUpdated
);

public record ChartDataResponse(
    string ChartId,
    string Type,
    string Title,
    List<DataPointResponse> DataPoints,
    DateTime GeneratedAt
);

public record DataPointResponse(
    string Label,
    decimal Value,
    string? Category,
    DateTime? Timestamp
);

public record AlertResponse(
    string Id,
    string Title,
    string Message,
    string Severity,
    string Source,
    Dictionary<string, object> Data,
    bool IsRead,
    DateTime CreatedAt
);

public record RevenueChartResponse(
    List<DataPointResponse> Daily,
    List<DataPointResponse> Weekly,
    List<DataPointResponse> Monthly
);

public record SalesOverviewResponse(
    int TotalOrders,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    int OrdersToday,
    int OrdersThisWeek,
    int OrdersThisMonth,
    List<TopProductResponse> TopProducts
);

public record TopProductResponse(
    string ProductId,
    string ProductName,
    int QuantitySold,
    decimal Revenue
);

public record InventoryOverviewResponse(
    int TotalProducts,
    int LowStockProducts,
    int OutOfStockProducts,
    decimal TotalInventoryValue,
    List<LowStockProductResponse> LowStockItems
);

public record LowStockProductResponse(
    string ProductId,
    string ProductName,
    int CurrentStock,
    int ReorderLevel
);

public record FinancialSummaryResponse(
    decimal TotalRevenue,
    decimal TotalExpenses,
    decimal NetIncome,
    decimal ProfitMargin,
    List<ExpenseByCategoryResponse> ExpensesByCategory,
    List<RevenueByMonthResponse> RevenueByMonth
);

public record ExpenseByCategoryResponse(
    string Category,
    decimal Amount
);

public record RevenueByMonthResponse(
    string Month,
    decimal Revenue,
    decimal Expenses,
    decimal Profit
);

// Kafka Event DTOs
public record UserEventDTO(
    string UserId,
    string Email,
    string Role,
    string EventType,
    DateTime Timestamp
);

public record ProductEventDTO(
    string ProductId,
    string Name,
    string Category,
    int StockLevel,
    decimal Price,
    string EventType,
    DateTime Timestamp
);

public record OrderEventDTO(
    string OrderId,
    string CustomerId,
    decimal TotalAmount,
    string Status,
    int ItemCount,
    string EventType,
    DateTime Timestamp
);

public record TransactionEventDTO(
    string TransactionId,
    string TransactionNumber,
    DateTime TransactionDate,
    string Type,
    decimal TotalAmount,
    string EventType,
    DateTime Timestamp
);

public record BudgetEventDTO(
    string BudgetId,
    string BudgetName,
    string AccountName,
    decimal BudgetAmount,
    decimal SpentAmount,
    decimal ExceededAmount,
    decimal PercentageUsed,
    DateTime Timestamp
);
