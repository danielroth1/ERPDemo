namespace FinancialManagement.Models;

public enum AccountCategory
{
    // Assets
    CurrentAssets,
    FixedAssets,
    OtherAssets,
    
    // Liabilities
    CurrentLiabilities,
    LongTermLiabilities,
    
    // Equity
    OwnersEquity,
    RetainedEarnings,
    
    // Revenue
    OperatingRevenue,
    NonOperatingRevenue,
    
    // Expenses
    CostOfGoodsSold,
    OperatingExpenses,
    NonOperatingExpenses
}
