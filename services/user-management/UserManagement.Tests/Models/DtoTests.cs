using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.Infrastructure;
using UserManagement.Models;
using UserManagement.Models.DTOs;
using UserManagement.Services;
using UserManagement.Tests.Helpers;

namespace UserManagement.Tests.Models;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ShouldBeTrue()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = null
        };

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenRevoked_ShouldBeFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow
        };

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenExpired_ShouldBeFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RevokedAt = null
        };

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenBothRevokedAndExpired_ShouldBeFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RevokedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var token = new RefreshToken();

        token.UserId.Should().BeEmpty();
        token.Token.Should().BeEmpty();
        token.RevokedAt.Should().BeNull();
        token.ReplacedByToken.Should().BeNull();
    }
}

public class ApiResponseTests
{
    [Fact]
    public void SuccessResponse_ShouldCreateSuccessfulResponse()
    {
        var response = ApiResponse<string>.SuccessResponse("data", "message");

        response.Success.Should().BeTrue();
        response.Data.Should().Be("data");
        response.Message.Should().Be("message");
    }

    [Fact]
    public void ErrorResponse_ShouldCreateFailedResponse()
    {
        var response = ApiResponse<string>.ErrorResponse("error message");

        response.Success.Should().BeFalse();
        response.Message.Should().Be("error message");
    }

    [Fact]
    public void SuccessResponse_WithNullMessage_ShouldHaveNullMessage()
    {
        var response = ApiResponse<string>.SuccessResponse("data");

        response.Message.Should().BeNull();
    }
}

public class UserResponseDtoTests
{
    [Fact]
    public void UserResponse_DefaultValues_ShouldBeCorrect()
    {
        var response = new UserResponse();

        response.Id.Should().BeEmpty();
        response.Email.Should().BeEmpty();
        response.FirstName.Should().BeEmpty();
        response.LastName.Should().BeEmpty();
        response.Roles.Should().BeEmpty();
        response.IsActive.Should().BeFalse();
    }
}
