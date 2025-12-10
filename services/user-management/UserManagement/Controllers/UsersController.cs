using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Models;
using UserManagement.Models.DTOs;
using UserManagement.Services;

namespace UserManagement.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserResponse>>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(ApiResponse<PaginatedResponse<UserResponse>>.ErrorResponse("Invalid pagination parameters"));
        }

        var users = await _userService.GetAllAsync(page, pageSize);
        var totalCount = await _userService.GetTotalCountAsync();
        var items = users.Select(u => _userService.MapToResponse(u)).ToList();

        var paginatedResponse = new PaginatedResponse<UserResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };

        return Ok(ApiResponse<PaginatedResponse<UserResponse>>.SuccessResponse(paginatedResponse));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(string id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound(ApiResponse<UserResponse>.ErrorResponse("User not found"));
        }

        return Ok(ApiResponse<UserResponse>.SuccessResponse(_userService.MapToResponse(user)));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(string id, [FromBody] User user)
    {
        var existingUser = await _userService.GetByIdAsync(id);

        if (existingUser == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
        }

        user.Id = id;
        user.CreatedAt = existingUser.CreatedAt;
        user.PasswordHash = existingUser.PasswordHash; // Don't allow password change via this endpoint

        var success = await _userService.UpdateAsync(id, user);

        if (success)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "User updated successfully"));
        }

        return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to update user"));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(string id)
    {
        var success = await _userService.DeleteAsync(id);

        if (success)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "User deleted successfully"));
        }

        return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(string id)
    {
        var success = await _userService.DeactivateUserAsync(id);

        if (success)
        {
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "User deactivated successfully"));
        }

        return NotFound(ApiResponse<object>.ErrorResponse("User not found"));
    }
}
