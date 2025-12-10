using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models;
using UserManagement.Models.DTOs;
using UserManagement.Services;
using BC = BCrypt.Net.BCrypt;

namespace UserManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserService userService,
        JwtService jwtService,
        EmailService emailService,
        ILogger<AuthController> logger)
    {
        _userService = userService;
        _jwtService = jwtService;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
    {
        if (await _userService.EmailExistsAsync(request.Email))
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Email already registered"));
        }

        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = BC.HashPassword(request.Password),
            Roles = new List<Role> { Role.User, Role.Manager, Role.Admin }, // everyone is Admin for now
            IsActive = true,
            EmailConfirmed = false
        };

        await _userService.CreateAsync(user);

        // Send welcome email
        try
        {
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = await _jwtService.GenerateRefreshTokenAsync(user.Id);

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = _userService.MapToResponse(user)
        }, "Registration successful"));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);

        if (user == null || !BC.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid email or password"));
        }

        if (!user.IsActive)
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Account is deactivated"));
        }

        // Update last login
        await _userService.UpdateLastLoginAsync(user.Id);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = await _jwtService.GenerateRefreshTokenAsync(user.Id);

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = _userService.MapToResponse(user)
        }, "Login successful"));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = await _jwtService.GetRefreshTokenAsync(request.RefreshToken);

        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid or expired refresh token"));
        }

        var user = await _userService.GetByIdAsync(refreshToken.UserId);

        if (user == null || !user.IsActive)
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("User not found or deactivated"));
        }

        // Generate new tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = await _jwtService.GenerateRefreshTokenAsync(user.Id);

        // Revoke old refresh token
        await _jwtService.RevokeRefreshTokenAsync(refreshToken.Token, newRefreshToken.Token);

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = _userService.MapToResponse(user)
        }, "Token refreshed successfully"));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));
        }

        // Revoke all user's refresh tokens
        await _jwtService.RevokeAllUserTokensAsync(userId);

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Logged out successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<UserResponse>.ErrorResponse("Unauthorized"));
        }

        var user = await _userService.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(ApiResponse<UserResponse>.ErrorResponse("User not found"));
        }

        return Ok(ApiResponse<UserResponse>.SuccessResponse(_userService.MapToResponse(user)));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized"));
        }

        var user = await _userService.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
        }

        // Verify current password
        if (!BC.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Current password is incorrect"));
        }

        // Update password
        user.PasswordHash = BC.HashPassword(request.NewPassword);
        await _userService.UpdateAsync(userId, user);

        // Revoke all refresh tokens
        await _jwtService.RevokeAllUserTokensAsync(userId);

        // Send notification email
        try
        {
            await _emailService.SendPasswordChangedEmailAsync(user.Email, user.FirstName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send password changed email to {Email}", user.Email);
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Password changed successfully"));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);

        if (user == null)
        {
            // Don't reveal that user doesn't exist
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "If the email exists, a password reset link has been sent"));
        }

        // Generate reset token (simplified - in production, store this securely)
        var resetToken = Guid.NewGuid().ToString("N");

        // Send reset email
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to send password reset email"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(new { }, "If the email exists, a password reset link has been sent"));
    }
}
