

namespace InventoryManagement.Services;

/// <summary>
/// Service to initialize and manage default financial accounts
/// </summary>
public interface IFinancialAccountInitializer
{
    Task<string?> GetOrCreateRevenueAccountIdAsync(string authToken);
    Task<string?> GetOrCreateCompanyAccountIdAsync(string authToken);
    Task<string?> GetOrCreateTaxAccountIdAsync(string authToken);
    Task<string?> GetOrCreateInventoryAccountIdAsync(string authToken);
    Task<string?> GetOrCreateProductExpenseAccountIdAsync(string authToken);
}

public class FinancialAccountInitializer : IFinancialAccountInitializer
{
    private readonly IFinancialServiceClient _financialClient;
    private readonly ILogger<FinancialAccountInitializer> _logger;
    private string? _cachedRevenueAccountId;
    private string? _cachedCompanyAccountId;
    private string? _cachedTaxAccountId;
    private string? _cachedInventoryAccountId;
    private string? _cachedProductExpenseAccountId;

    public FinancialAccountInitializer(
        IFinancialServiceClient financialClient,
        ILogger<FinancialAccountInitializer> logger)
    {
        _financialClient = financialClient;
        _logger = logger;
    }

    public async Task<string?> GetOrCreateRevenueAccountIdAsync(string authToken)
    {
        if (!string.IsNullOrEmpty(_cachedRevenueAccountId))
        {
            return _cachedRevenueAccountId;
        }

        try
        {
            var accountId = await _financialClient.GetSystemAccountIdAsync("revenue", authToken);

            if (!string.IsNullOrEmpty(accountId))
            {
                _cachedRevenueAccountId = accountId;
                _logger.LogInformation("Retrieved revenue account ID: {AccountId}", accountId);
                return _cachedRevenueAccountId;
            }

            _logger.LogWarning("Revenue account not found in Financial service. Using placeholder.");
            return "REVENUE_ACCOUNT";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get revenue account ID");
            return "REVENUE_ACCOUNT";
        }
    }

    public async Task<string?> GetOrCreateCompanyAccountIdAsync(string authToken)
    {
        if (!string.IsNullOrEmpty(_cachedCompanyAccountId))
        {
            return _cachedCompanyAccountId;
        }

        try
        {
            var accountId = await _financialClient.GetSystemAccountIdAsync("company-operating", authToken);

            if (!string.IsNullOrEmpty(accountId))
            {
                _cachedCompanyAccountId = accountId;
                _logger.LogInformation("Retrieved company account ID: {AccountId}", accountId);
                return _cachedCompanyAccountId;
            }

            _logger.LogWarning("Company account not found in Financial service. Using placeholder.");
            return "COMPANY_ACCOUNT";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get company account ID");
            return "COMPANY_ACCOUNT";
        }
    }

    public async Task<string?> GetOrCreateTaxAccountIdAsync(string authToken)
    {
        if (!string.IsNullOrEmpty(_cachedTaxAccountId))
        {
            return _cachedTaxAccountId;
        }

        try
        {
            var accountId = await _financialClient.GetSystemAccountIdAsync("sales-tax", authToken);

            if (!string.IsNullOrEmpty(accountId))
            {
                _cachedTaxAccountId = accountId;
                _logger.LogInformation("Retrieved tax account ID: {AccountId}", accountId);
                return _cachedTaxAccountId;
            }

            _logger.LogWarning("Tax account not found in Financial service. Using placeholder.");
            return "TAX_ACCOUNT";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tax account ID");
            return "TAX_ACCOUNT";
        }
    }

    public async Task<string?> GetOrCreateInventoryAccountIdAsync(string authToken)
    {
        if (!string.IsNullOrEmpty(_cachedInventoryAccountId))
        {
            return _cachedInventoryAccountId;
        }

        try
        {
            var accountId = await _financialClient.GetSystemAccountIdAsync("inventory", authToken);

            if (!string.IsNullOrEmpty(accountId))
            {
                _cachedInventoryAccountId = accountId;
                _logger.LogInformation("Retrieved inventory account ID: {AccountId}", accountId);
                return _cachedInventoryAccountId;
            }

            _logger.LogWarning("Inventory account not found in Financial service. Using placeholder.");
            return "INVENTORY_ACCOUNT";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inventory account ID");
            return "INVENTORY_ACCOUNT";
        }
    }

    public async Task<string?> GetOrCreateProductExpenseAccountIdAsync(string authToken)
    {
        if (!string.IsNullOrEmpty(_cachedProductExpenseAccountId))
        {
            return _cachedProductExpenseAccountId;
        }

        try
        {
            var accountId = await _financialClient.GetSystemAccountIdAsync("cogs", authToken);

            if (!string.IsNullOrEmpty(accountId))
            {
                _cachedProductExpenseAccountId = accountId;
                _logger.LogInformation("Retrieved product expense account ID: {AccountId}", accountId);
                return _cachedProductExpenseAccountId;
            }

            _logger.LogWarning("Product expense account not found in Financial service. Using placeholder.");
            return "PRODUCT_EXPENSE_ACCOUNT";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get product expense account ID");
            return "PRODUCT_EXPENSE_ACCOUNT";
        }
    }
}
