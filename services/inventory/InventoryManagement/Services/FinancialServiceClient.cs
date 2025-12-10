using System.Text;
using System.Text.Json;
using InventoryManagement.Models.DTOs;

namespace InventoryManagement.Services;

/// <summary>
/// Client for communicating with the Financial Management service
/// </summary>
public interface IFinancialServiceClient
{
    Task<string?> GetUserAccountIdAsync(string userId, string authToken);
    Task<string?> GetAccountIdByNumberAsync(string accountNumber, string authToken);
    Task<string?> GetRevenueAccountIdAsync(string authToken);
    Task<bool> CreateTransactionAsync(CreateFinancialTransactionRequest request, string authToken);
}

public class FinancialServiceClient : IFinancialServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FinancialServiceClient> _logger;
    private readonly string _baseUrl;

    public FinancialServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<FinancialServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["Services:Financial"] ?? "http://financial:8080";
    }

    public async Task<string?> GetUserAccountIdAsync(string userId, string authToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/v1/accounts/user/{userId}");
            
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Add("Authorization", authToken);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var accountResponse = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (accountResponse.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("id", out var accountId))
                {
                    return accountId.GetString();
                }
            }
            else
            {
                _logger.LogWarning(
                    "Failed to get user account. UserId: {UserId}, Status: {StatusCode}",
                    userId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting user account for UserId: {UserId}", userId);
        }
        
        return null;
    }

    public async Task<string?> GetAccountIdByNumberAsync(string accountNumber, string authToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/v1/accounts/number/{accountNumber}");
            
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Add("Authorization", authToken);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var accountResponse = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (accountResponse.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("id", out var accountId))
                {
                    return accountId.GetString();
                }
            }
            else
            {
                _logger.LogWarning(
                    "Failed to get account by number. AccountNumber: {AccountNumber}, Status: {StatusCode}",
                    accountNumber, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting account by number: {AccountNumber}", accountNumber);
        }
        
        return null;
    }

    public async Task<string?> GetRevenueAccountIdAsync(string authToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/v1/accounts");
            
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Add("Authorization", authToken);
            }

            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var accountsResponse = JsonSerializer.Deserialize<JsonElement>(content);
                
                if (accountsResponse.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    // Look for revenue account with name "Product Sales Revenue"
                    foreach (var account in data.EnumerateArray())
                    {
                        if (account.TryGetProperty("type", out var type) &&
                            type.GetString() == "Revenue" &&
                            account.TryGetProperty("category", out var category) &&
                            category.GetString() == "Operating" &&
                            account.TryGetProperty("name", out var name) &&
                            name.GetString() == "Product Sales Revenue" &&
                            account.TryGetProperty("id", out var accountId))
                        {
                            return accountId.GetString();
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning(
                    "Failed to get accounts list. Status: {StatusCode}",
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while getting revenue account");
        }
        
        return null;
    }

    public async Task<bool> CreateTransactionAsync(CreateFinancialTransactionRequest request, string authToken)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/transactions")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(authToken))
            {
                httpRequest.Headers.Add("Authorization", authToken);
            }

            var response = await _httpClient.SendAsync(httpRequest);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Financial transaction created successfully");
                return true;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Failed to create financial transaction. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating financial transaction");
            return false;
        }
    }
}
