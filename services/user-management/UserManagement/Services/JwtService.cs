using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using UserManagement.Configuration;
using UserManagement.Infrastructure;
using UserManagement.Models;

namespace UserManagement.Services;

public class JwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IMongoCollection<RefreshToken> _refreshTokens;
    private readonly ILogger<JwtService> _logger;

    public JwtService(
        JwtSettings jwtSettings,
        MongoDbContext dbContext,
        ILogger<JwtService> logger)
    {
        _jwtSettings = jwtSettings;
        _refreshTokens = dbContext.GetCollection<RefreshToken>("refreshTokens");
        _logger = logger;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add roles
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = GenerateSecureRandomToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokens.InsertOneAsync(refreshToken);

        _logger.LogInformation("Refresh token generated for user: {UserId}", userId);

        return refreshToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _refreshTokens.Find(rt => rt.Token == token).FirstOrDefaultAsync();
    }

    public async Task<bool> RevokeRefreshTokenAsync(string token, string? replacedByToken = null)
    {
        var update = Builders<RefreshToken>.Update
            .Set(rt => rt.RevokedAt, DateTime.UtcNow)
            .Set(rt => rt.ReplacedByToken, replacedByToken);

        var result = await _refreshTokens.UpdateOneAsync(rt => rt.Token == token, update);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("Refresh token revoked: {Token}", token);
            return true;
        }

        return false;
    }

    public async Task RevokeAllUserTokensAsync(string userId)
    {
        var update = Builders<RefreshToken>.Update.Set(rt => rt.RevokedAt, DateTime.UtcNow);

        await _refreshTokens.UpdateManyAsync(
            rt => rt.UserId == userId && rt.RevokedAt == null,
            update
        );

        _logger.LogInformation("All refresh tokens revoked for user: {UserId}", userId);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = false, // Don't validate expiration here
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    private static string GenerateSecureRandomToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
