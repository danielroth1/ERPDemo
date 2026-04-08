using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ERP.Testing.Shared.Auth;

/// <summary>
/// Generates JWT tokens for integration tests with configurable claims.
/// Uses the same signing parameters the services expect.
/// </summary>
public static class TestJwtTokenHelper
{
    public const string TestSecret = "test-jwt-secret-key-that-is-long-enough-for-hmac-sha256-!@#$";
    public const string TestIssuer = "erp-test";
    public const string TestAudience = "erp-test";

    public static string GenerateToken(
        string userId = "test-user-id",
        string email = "test@example.com",
        string username = "testuser",
        string[] roles = null!,
        TimeSpan? expiry = null)
    {
        roles ??= ["Admin"];
        expiry ??= TimeSpan.FromHours(1);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, username),
            new("userId", userId),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiry.Value),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient SetBearerToken(this HttpClient client, string? token = null)
    {
        token ??= GenerateToken();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
