

namespace InventoryManagement.Services;

/// <summary>
/// Service to initialize and manage default financial accounts
/// </summary>
public interface IFinancialAccountInitializer
{
    Task<string?> GetOrCreateRevenueAccountIdAsync();
}

public class FinancialAccountInitializer : IFinancialAccountInitializer
{
    private readonly IFinancialServiceClient _financialClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FinancialAccountInitializer> _logger;
    private string? _cachedRevenueAccountId;

    public FinancialAccountInitializer(
        IFinancialServiceClient financialClient,
        IConfiguration configuration,
        ILogger<FinancialAccountInitializer> logger)
    {
        _financialClient = financialClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetOrCreateRevenueAccountIdAsync()
    {
        // Return cached ID if available
        if (!string.IsNullOrEmpty(_cachedRevenueAccountId))
        {
            return _cachedRevenueAccountId;
        }

        // Try to get from configuration
        var configuredId = _configuration["Financial:RevenueAccountId"];
        if (!string.IsNullOrEmpty(configuredId) && configuredId != "REVENUE_ACCOUNT")
        {
            _cachedRevenueAccountId = configuredId;
            return _cachedRevenueAccountId;
        }

        // Try to get revenue account from Financial service
        try
        {
            var accountId = await _financialClient.GetRevenueAccountIdAsync(string.Empty);
            
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
}
