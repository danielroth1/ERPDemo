using Microsoft.EntityFrameworkCore;
using MassTransit;
using UserManagement.Infrastructure;
using UserManagement.Models;
using UserManagement.Models.DTOs;
using ERP.Contracts.Events.Domain;

namespace UserManagement.Services;

public class UserService
{
    private readonly AppDbContext _dbContext;
    private readonly ITopicProducer<UserCreated> _userCreatedProducer;
    private readonly ITopicProducer<UserUpdated> _userUpdatedProducer;
    private readonly ITopicProducer<UserDeleted> _userDeletedProducer;
    private readonly ITopicProducer<UserDeactivated> _userDeactivatedProducer;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AppDbContext dbContext,
        ITopicProducer<UserCreated> userCreatedProducer,
        ITopicProducer<UserUpdated> userUpdatedProducer,
        ITopicProducer<UserDeleted> userDeletedProducer,
        ITopicProducer<UserDeactivated> userDeactivatedProducer,
        ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _userCreatedProducer = userCreatedProducer;
        _userUpdatedProducer = userUpdatedProducer;
        _userDeletedProducer = userDeletedProducer;
        _userDeactivatedProducer = userDeactivatedProducer;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _dbContext.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<List<User>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _dbContext.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _dbContext.Users.CountAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Id = Guid.NewGuid().ToString();
        user.Email = user.Email.ToLower();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User created: {UserId} - {Email}", user.Id, user.Email);

        // Publish event via MassTransit
        await _userCreatedProducer.Produce(new UserCreated
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = string.Join(",", user.Roles)
        });

        return user;
    }

    public async Task<bool> UpdateAsync(string id, User user)
    {
        var existingUser = await _dbContext.Users.FindAsync(id);
        if (existingUser == null) return false;

        existingUser.Email = user.Email;
        existingUser.FirstName = user.FirstName;
        existingUser.LastName = user.LastName;
        existingUser.Roles = user.Roles;
        existingUser.IsActive = user.IsActive;
        existingUser.EmailConfirmed = user.EmailConfirmed;
        existingUser.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User updated: {UserId}", id);

        await _userUpdatedProducer.Produce(new UserUpdated
        {
            UserId = id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = string.Join(",", user.Roles)
        });

        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return false;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User deleted: {UserId}", id);

        await _userDeletedProducer.Produce(new UserDeleted { UserId = id });

        return true;
    }

    public async Task<bool> UpdateLastLoginAsync(string id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return false;

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeactivateUserAsync(string id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User deactivated: {UserId}", id);

        await _userDeactivatedProducer.Produce(new UserDeactivated { UserId = id });

        return true;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = user.Roles,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
