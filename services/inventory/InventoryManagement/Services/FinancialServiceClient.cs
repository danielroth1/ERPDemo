using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Abstractions;
using InventoryManagement.Models.DTOs;
using GeneratedFinancialClient = InventoryManagement.Generated.Clients.Financial.FinancialServiceClient;

namespace InventoryManagement.Services;

/// <summary>
/// Client for communicating with the Financial Management service using Kiota-generated API client
/// </summary>
public interface IFinancialServiceClient
{
    Task<string?> GetUserAccountIdAsync(string userId, string authToken);
    Task<string?> GetUserExpenseAccountIdAsync(string userId, string authToken);
    Task<string?> GetAccountIdByNumberAsync(string accountNumber, string authToken);
    Task<string?> GetAccountIdByNameAsync(string accountName, string authToken);
    Task<string?> GetRevenueAccountIdAsync(string authToken);
    Task<string?> GetSystemAccountIdAsync(string purpose, string authToken);
    Task<bool> CreateTransactionAsync(CreateFinancialTransactionRequest request, string authToken);
}

public class FinancialServiceClientWrapper : IFinancialServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FinancialServiceClientWrapper> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _baseUrl;

    public FinancialServiceClientWrapper(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FinancialServiceClientWrapper> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = configuration["Services:Financial"] ?? "http://financial:8080";
    }

    /// <summary>
    /// Create Kiota request adapter with authentication
    /// </summary>
    private GeneratedFinancialClient CreateKiotaClient(string? authToken = null)
    {
        var httpClient = _httpClientFactory.CreateClient("FinancialService");
        httpClient.BaseAddress = new Uri(_baseUrl);
        
        if (!string.IsNullOrEmpty(authToken))
        {
            httpClient.DefaultRequestHeaders.Add("Authorization", authToken);
        }

        // Create anonymous auth provider (services trust each other in internal network)
        var authProvider = new AnonymousAuthenticationProvider();
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        adapter.BaseUrl = _baseUrl;

        return new GeneratedFinancialClient(adapter);
    }

    public async Task<string?> GetUserExpenseAccountIdAsync(string userId, string authToken)
    {
        try
        {
            var client = CreateKiotaClient(authToken);
            var response = await client.Api.V1.Accounts.User[userId].Expense.GetAsync();

            return response?.Data?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting user expense account for UserId: {UserId}", userId);
            return null;
        }
    }

    public async Task<string?> GetUserAccountIdAsync(string userId, string authToken)
    {
        try
        {
            var client = CreateKiotaClient(authToken);
            var response = await client.Api.V1.Accounts.User[userId].GetAsync();

            return response?.Data?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting user account for UserId: {UserId}", userId);
            return null;
        }
    }

    public async Task<string?> GetAccountIdByNumberAsync(string accountNumber, string authToken)
    {
        try
        {
            var client = CreateKiotaClient(authToken);
            var response = await client.Api.V1.Accounts.Number[accountNumber].GetAsync();

            return response?.Data?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting account by number: {AccountNumber}", accountNumber);
            return null;
        }
    }

    public async Task<string?> GetAccountIdByNameAsync(string accountName, string authToken)
    {
        try
        {
            var client = CreateKiotaClient(authToken);
            var response = await client.Api.V1.Accounts.Name[Uri.EscapeDataString(accountName)].GetAsync();

            return response?.Data?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting account by name: {AccountName}", accountName);
            return null;
        }
    }

    public async Task<string?> GetRevenueAccountIdAsync(string authToken)
    {
        try
        {
            var client = CreateKiotaClient(authToken);
            var response = await client.Api.V1.Accounts.GetAsync();

            if (response?.Data != null)
            {
                // Look for revenue account with name "Product Sales Revenue"
                var revenueAccount = response.Data.FirstOrDefault(a =>
                    a.Type == "Revenue" &&
                    a.Category == "Operating" &&
                    a.Name == "Product Sales Revenue");

                return revenueAccount?.Id;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting revenue account");
            return null;
        }
    }

    public async Task<string?> GetSystemAccountIdAsync(string purpose, string authToken)
    {
        try
        {
            var client = CreateKiotaClient(authToken);
            var response = await client.Api.V1.Accounts.System[purpose].GetAsync();

            return response?.Data?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting system account: {Purpose}", purpose);
            return null;
        }
    }

    public async Task<bool> CreateTransactionAsync(CreateFinancialTransactionRequest request, string authToken)
    {
        try
        {
            var client = CreateKiotaClient(authToken);
            
            // Map to Kiota-generated request model
            var kiotaRequest = new Generated.Clients.Financial.Models.CreateTransactionRequest
            {
                Description = request.Description,
                Type = request.Type,  // IMPORTANT: Must include transaction type
                Date = DateTimeOffset.UtcNow,  // DateTimeOffset automatically preserves UTC
                ReferenceId = request.ReferenceId,
                ReferenceType = request.ReferenceType,
                Entries = request.Entries.Select(e => new Generated.Clients.Financial.Models.JournalEntryRequest
                {
                    AccountId = e.AccountId,
                    Debit = (double)e.Debit,
                    Credit = (double)e.Credit,
                    Memo = e.Memo
                }).ToList()
            };

            var response = await client.Api.V1.Transactions.PostAsync(kiotaRequest);

            if (response?.Success == true)
            {
                _logger.LogInformation("Financial transaction created successfully. Type: {Type}, Reference: {ReferenceId}", request.Type, request.ReferenceId);
                return true;
            }

            _logger.LogWarning("Failed to create financial transaction: {Message}", response?.Message);
            return false;
        }
        catch (Microsoft.Kiota.Abstractions.ApiException apiEx)
        {
            // Kiota doesn't expose the raw response body, only the status code and message
            _logger.LogError(apiEx, 
                "API error while creating financial transaction. " +
                "Status: {StatusCode}, Message: {ErrorMessage}. " +
                "Check Financial service logs for details. " +
                "Transaction Type: {Type}, Description: {Description}", 
                apiEx.ResponseStatusCode, 
                apiEx.Message,
                request.Type,
                request.Description);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating financial transaction");
            return false;
        }
    }
}
