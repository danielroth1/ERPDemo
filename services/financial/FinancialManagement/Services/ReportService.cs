using Microsoft.EntityFrameworkCore;
using FinancialManagement.Infrastructure;
using FinancialManagement.Models;
using FinancialManagement.Models.DTOs;

namespace FinancialManagement.Services;

public interface IReportService
{
    Task<BalanceSheetResponse> GenerateBalanceSheetAsync(DateTime asOfDate);
    Task<IncomeStatementResponse> GenerateIncomeStatementAsync(DateTime startDate, DateTime endDate);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReportService> _logger;

    public ReportService(AppDbContext dbContext, ILogger<ReportService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BalanceSheetResponse> GenerateBalanceSheetAsync(DateTime asOfDate)
    {
        _logger.LogInformation("Generating balance sheet as of {AsOfDate}", asOfDate);

        var accounts = await _dbContext.Accounts
            .Where(a => a.IsActive)
            .ToListAsync();

        var assets = new AssetSection();
        var liabilities = new LiabilitySection();
        var equity = new EquitySection();

        foreach (var account in accounts)
        {
            var accountBalance = new AccountBalance
            {
                AccountId = account.Id,
                AccountNumber = account.AccountNumber,
                AccountName = account.Name,
                Balance = account.Balance
            };

            switch (account.Type)
            {
                case AccountType.Asset:
                    switch (account.Category)
                    {
                        case AccountCategory.CurrentAssets:
                            assets.CurrentAssets.Add(accountBalance);
                            break;
                        case AccountCategory.FixedAssets:
                            assets.FixedAssets.Add(accountBalance);
                            break;
                        case AccountCategory.OtherAssets:
                            assets.OtherAssets.Add(accountBalance);
                            break;
                    }
                    break;

                case AccountType.Liability:
                    switch (account.Category)
                    {
                        case AccountCategory.CurrentLiabilities:
                            liabilities.CurrentLiabilities.Add(accountBalance);
                            break;
                        case AccountCategory.LongTermLiabilities:
                            liabilities.LongTermLiabilities.Add(accountBalance);
                            break;
                    }
                    break;

                case AccountType.Equity:
                    equity.EquityAccounts.Add(accountBalance);
                    break;
            }
        }

        assets.Total = assets.CurrentAssets.Sum(a => a.Balance) +
                      assets.FixedAssets.Sum(a => a.Balance) +
                      assets.OtherAssets.Sum(a => a.Balance);

        liabilities.Total = liabilities.CurrentLiabilities.Sum(l => l.Balance) +
                           liabilities.LongTermLiabilities.Sum(l => l.Balance);

        equity.Total = equity.EquityAccounts.Sum(e => e.Balance);

        return new BalanceSheetResponse
        {
            AsOfDate = asOfDate,
            Assets = assets,
            Liabilities = liabilities,
            Equity = equity,
            TotalAssets = assets.Total,
            TotalLiabilities = liabilities.Total,
            TotalEquity = equity.Total
        };
    }

    public async Task<IncomeStatementResponse> GenerateIncomeStatementAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating income statement from {StartDate} to {EndDate}", 
            startDate, endDate);

        var accounts = await _dbContext.Accounts
            .Where(a => a.IsActive && (a.Type == AccountType.Revenue || a.Type == AccountType.Expense))
            .ToListAsync();

        var revenue = new List<AccountBalance>();
        var expenses = new List<AccountBalance>();

        // Get transactions in date range to calculate period balances
        var transactions = await _dbContext.Transactions
            .Where(t => t.Status == TransactionStatus.Posted &&
                       t.Date >= startDate &&
                       t.Date <= endDate)
            .ToListAsync();

        // Calculate account balances for the period
        var accountBalances = new Dictionary<string, decimal>();

        foreach (var transaction in transactions)
        {
            foreach (var entry in transaction.Entries)
            {
                if (!accountBalances.ContainsKey(entry.AccountId))
                {
                    accountBalances[entry.AccountId] = 0;
                }

                var account = accounts.FirstOrDefault(a => a.Id == entry.AccountId);
                if (account != null)
                {
                    // Revenue increases with credits, Expenses increase with debits
                    var balanceChange = account.Type == AccountType.Revenue
                        ? entry.Credit - entry.Debit
                        : entry.Debit - entry.Credit;

                    accountBalances[entry.AccountId] += balanceChange;
                }
            }
        }

        foreach (var account in accounts)
        {
            var balance = accountBalances.ContainsKey(account.Id) ? accountBalances[account.Id] : 0;

            var accountBalance = new AccountBalance
            {
                AccountId = account.Id,
                AccountNumber = account.AccountNumber,
                AccountName = account.Name,
                Balance = balance
            };

            if (account.Type == AccountType.Revenue)
            {
                revenue.Add(accountBalance);
            }
            else if (account.Type == AccountType.Expense)
            {
                expenses.Add(accountBalance);
            }
        }

        var totalRevenue = revenue.Sum(r => r.Balance);
        var totalExpenses = expenses.Sum(e => e.Balance);
        var netIncome = totalRevenue - totalExpenses;

        return new IncomeStatementResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            Revenue = revenue,
            Expenses = expenses,
            TotalRevenue = totalRevenue,
            TotalExpenses = totalExpenses,
            NetIncome = netIncome
        };
    }
}
