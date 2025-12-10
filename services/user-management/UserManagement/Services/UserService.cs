using MongoDB.Driver;
using UserManagement.Configuration;
using UserManagement.Infrastructure;
using UserManagement.Models;
using UserManagement.Models.DTOs;

namespace UserManagement.Services;

public class UserService
{
    private readonly IMongoCollection<User> _users;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<UserService> _logger;

    public UserService(
        MongoDbContext dbContext,
        KafkaProducer kafkaProducer,
        ILogger<UserService> logger)
    {
        _users = dbContext.GetCollection<User>("users");
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email.ToLower() == email.ToLower()).FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        return await _users
            .Find(_ => true)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync()
    {
        return (int)await _users.CountDocumentsAsync(_ => true);
    }

    public async Task<User> CreateAsync(User user)
    {
        user.Email = user.Email.ToLower();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _users.InsertOneAsync(user);

        _logger.LogInformation("User created: {UserId} - {Email}", user.Id, user.Email);

        // Publish event to Kafka
        await _kafkaProducer.PublishEventAsync("UserCreated", new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Roles
        });

        return user;
    }

    public async Task<bool> UpdateAsync(string id, User user)
    {
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _users.ReplaceOneAsync(u => u.Id == id, user);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("User updated: {UserId}", id);

            await _kafkaProducer.PublishEventAsync("UserUpdated", new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.IsActive
            });

            return true;
        }

        return false;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _users.DeleteOneAsync(u => u.Id == id);

        if (result.DeletedCount > 0)
        {
            _logger.LogInformation("User deleted: {UserId}", id);

            await _kafkaProducer.PublishEventAsync("UserDeleted", new { UserId = id });

            return true;
        }

        return false;
    }

    public async Task<bool> UpdateLastLoginAsync(string id)
    {
        var update = Builders<User>.Update
            .Set(u => u.LastLoginAt, DateTime.UtcNow)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _users.UpdateOneAsync(u => u.Id == id, update);

        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeactivateUserAsync(string id)
    {
        var update = Builders<User>.Update
            .Set(u => u.IsActive, false)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        var result = await _users.UpdateOneAsync(u => u.Id == id, update);

        if (result.ModifiedCount > 0)
        {
            _logger.LogInformation("User deactivated: {UserId}", id);

            await _kafkaProducer.PublishEventAsync("UserDeactivated", new { UserId = id });

            return true;
        }

        return false;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var count = await _users.CountDocumentsAsync(u => u.Email.ToLower() == email.ToLower());
        return count > 0;
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
