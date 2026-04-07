using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserManagement.Configuration;
using UserManagement.Infrastructure;
using UserManagement.Models;
using UserManagement.Services;
using UserManagement.Tests.Helpers;

namespace UserManagement.Tests.Services;

public class JwtServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly JwtSettings _jwtSettings;
    private readonly JwtService _service;

    public JwtServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _jwtSettings = new JwtSettings
        {
            Secret = "ThisIsAVeryLongSecretKeyForTestingPurposesOnly123456789!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
        _service = new JwtService(
            _jwtSettings,
            _dbContext,
            new Mock<ILogger<JwtService>>().Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private User CreateTestUser(string? id = null) => new()
    {
        Id = id ?? "user-1",
        Email = "alice@example.com",
        FirstName = "Alice",
        LastName = "Smith",
        Roles = new List<Role> { Role.User, Role.Admin },
        IsActive = true
    };

    // ==================== GenerateAccessToken ====================

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtToken()
    {
        var user = CreateTestUser();

        var token = _service.GenerateAccessToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainUserClaims()
    {
        var user = CreateTestUser(id: "user-123");

        var token = _service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.NameIdentifier && c.Value == "user-123");
        jwtToken.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Email && c.Value == "alice@example.com");
        jwtToken.Claims.Should().Contain(c =>
            c.Type == ClaimTypes.Name && c.Value == "Alice Smith");
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainRoleClaims()
    {
        var user = CreateTestUser();
        user.Roles = new List<Role> { Role.User, Role.Admin };

        var token = _service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().Contain(Role.User.ToString());
        roleClaims.Should().Contain(Role.Admin.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveCorrectIssuerAndAudience()
    {
        var user = CreateTestUser();

        var token = _service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("TestIssuer");
        jwtToken.Audiences.Should().Contain("TestAudience");
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveCorrectExpiration()
    {
        var user = CreateTestUser();

        var token = _service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainUniqueJti()
    {
        var user = CreateTestUser();

        var token1 = _service.GenerateAccessToken(user);
        var token2 = _service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jti1 = handler.ReadJwtToken(token1).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = handler.ReadJwtToken(token2).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }

    // ==================== GenerateRefreshTokenAsync ====================

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldCreateRefreshToken()
    {
        var result = await _service.GenerateRefreshTokenAsync("user-1");

        result.Should().NotBeNull();
        result.UserId.Should().Be("user-1");
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldSetCorrectExpiration()
    {
        var result = await _service.GenerateRefreshTokenAsync("user-1");

        result.ExpiresAt.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldPersistInDatabase()
    {
        var result = await _service.GenerateRefreshTokenAsync("user-1");

        var fromDb = _dbContext.RefreshTokens.FirstOrDefault(rt => rt.Token == result.Token);
        fromDb.Should().NotBeNull();
        fromDb!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_ShouldGenerateUniqueTokens()
    {
        var token1 = await _service.GenerateRefreshTokenAsync("user-1");
        var token2 = await _service.GenerateRefreshTokenAsync("user-1");

        token1.Token.Should().NotBe(token2.Token);
    }

    // ==================== GetRefreshTokenAsync ====================

    [Fact]
    public async Task GetRefreshTokenAsync_WithExistingToken_ShouldReturnRefreshToken()
    {
        var created = await _service.GenerateRefreshTokenAsync("user-1");

        var result = await _service.GetRefreshTokenAsync(created.Token);

        result.Should().NotBeNull();
        result!.UserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetRefreshTokenAsync_WithNonExistingToken_ShouldReturnNull()
    {
        var result = await _service.GetRefreshTokenAsync("nonexistent-token");

        result.Should().BeNull();
    }

    // ==================== RevokeRefreshTokenAsync ====================

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithExistingToken_ShouldRevokeAndReturnTrue()
    {
        var created = await _service.GenerateRefreshTokenAsync("user-1");

        var result = await _service.RevokeRefreshTokenAsync(created.Token);

        result.Should().BeTrue();
        var fromDb = _dbContext.RefreshTokens.First(rt => rt.Token == created.Token);
        fromDb.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithReplacedByToken_ShouldSetReplacedByToken()
    {
        var created = await _service.GenerateRefreshTokenAsync("user-1");

        await _service.RevokeRefreshTokenAsync(created.Token, "new-replacement-token");

        var fromDb = _dbContext.RefreshTokens.First(rt => rt.Token == created.Token);
        fromDb.ReplacedByToken.Should().Be("new-replacement-token");
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithNonExistingToken_ShouldReturnFalse()
    {
        var result = await _service.RevokeRefreshTokenAsync("nonexistent");

        result.Should().BeFalse();
    }

    // ==================== RevokeAllUserTokensAsync ====================

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllActiveTokens()
    {
        await _service.GenerateRefreshTokenAsync("user-1");
        await _service.GenerateRefreshTokenAsync("user-1");
        await _service.GenerateRefreshTokenAsync("user-1");

        await _service.RevokeAllUserTokensAsync("user-1");

        var tokens = _dbContext.RefreshTokens.Where(rt => rt.UserId == "user-1").ToList();
        tokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldNotAffectOtherUsersTokens()
    {
        await _service.GenerateRefreshTokenAsync("user-1");
        await _service.GenerateRefreshTokenAsync("user-2");

        await _service.RevokeAllUserTokensAsync("user-1");

        var user2Token = _dbContext.RefreshTokens.First(rt => rt.UserId == "user-2");
        user2Token.RevokedAt.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldNotRevokeAlreadyRevokedTokens()
    {
        var token = await _service.GenerateRefreshTokenAsync("user-1");
        await _service.RevokeRefreshTokenAsync(token.Token);
        var firstRevokedAt = _dbContext.RefreshTokens.First(rt => rt.Token == token.Token).RevokedAt;

        await _service.RevokeAllUserTokensAsync("user-1");

        // The already-revoked token should keep its original RevokedAt
        // (the query only selects where RevokedAt == null)
        var fromDb = _dbContext.RefreshTokens.First(rt => rt.Token == token.Token);
        fromDb.RevokedAt.Should().Be(firstRevokedAt);
    }

    // ==================== ValidateToken ====================

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        var user = CreateTestUser();
        var token = _service.GenerateAccessToken(user);

        var result = _service.ValidateToken(token);

        result.Should().NotBeNull();
        result!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("user-1");
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        var result = _service.ValidateToken("invalid-token-string");

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithDifferentSecret_ShouldReturnNull()
    {
        // Create a token with different settings
        var otherSettings = new JwtSettings
        {
            Secret = "ACompletelyDifferentSecretKeyForTesting123456789!!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        };
        var otherService = new JwtService(otherSettings, _dbContext,
            new Mock<ILogger<JwtService>>().Object);

        var token = otherService.GenerateAccessToken(CreateTestUser());

        var result = _service.ValidateToken(token);

        result.Should().BeNull();
    }

    // ==================== RefreshToken Model ====================

    [Fact]
    public void RefreshToken_IsActive_WhenNotRevokedAndNotExpired_ShouldBeTrue()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = null
        };

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RefreshToken_IsActive_WhenRevoked_ShouldBeFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow
        };

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_IsActive_WhenExpired_ShouldBeFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RevokedAt = null
        };

        token.IsActive.Should().BeFalse();
    }
}
