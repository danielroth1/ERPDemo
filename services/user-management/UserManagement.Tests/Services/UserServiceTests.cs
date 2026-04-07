using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.Infrastructure;
using UserManagement.Models;
using UserManagement.Models.DTOs;
using UserManagement.Services;
using UserManagement.Tests.Helpers;
using ERP.Contracts.Events.Domain;

namespace UserManagement.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ITopicProducer<UserCreated>> _userCreatedProducer;
    private readonly Mock<ITopicProducer<UserUpdated>> _userUpdatedProducer;
    private readonly Mock<ITopicProducer<UserDeleted>> _userDeletedProducer;
    private readonly Mock<ITopicProducer<UserDeactivated>> _userDeactivatedProducer;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _userCreatedProducer = new Mock<ITopicProducer<UserCreated>>();
        _userUpdatedProducer = new Mock<ITopicProducer<UserUpdated>>();
        _userDeletedProducer = new Mock<ITopicProducer<UserDeleted>>();
        _userDeactivatedProducer = new Mock<ITopicProducer<UserDeactivated>>();
        _loggerMock = new Mock<ILogger<UserService>>();

        _service = new UserService(
            _dbContext,
            _userCreatedProducer.Object,
            _userUpdatedProducer.Object,
            _userDeletedProducer.Object,
            _userDeactivatedProducer.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private User CreateUser(string? id = null, string? email = null, string? firstName = null,
        string? lastName = null, bool isActive = true) => new()
    {
        Id = id ?? Guid.NewGuid().ToString(),
        Email = email ?? $"user-{Guid.NewGuid():N}@example.com",
        PasswordHash = "hashed-password",
        FirstName = firstName ?? "John",
        LastName = lastName ?? "Doe",
        Roles = new List<Role> { Role.User },
        IsActive = isActive,
        EmailConfirmed = false,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private async Task<User> SeedUserAsync(User? user = null)
    {
        var u = user ?? CreateUser();
        _dbContext.Users.Add(u);
        await _dbContext.SaveChangesAsync();
        return u;
    }

    // ==================== GetByIdAsync ====================

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnUser()
    {
        var user = await SeedUserAsync(CreateUser(id: "user-1", firstName: "Alice"));

        var result = await _service.GetByIdAsync("user-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be("user-1");
        result.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        var result = await _service.GetByIdAsync("nonexistent");

        result.Should().BeNull();
    }

    // ==================== GetByEmailAsync ====================

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
    {
        await SeedUserAsync(CreateUser(email: "alice@example.com", firstName: "Alice"));

        var result = await _service.GetByEmailAsync("alice@example.com");

        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetByEmailAsync_CaseInsensitive_ShouldReturnUser()
    {
        await SeedUserAsync(CreateUser(email: "alice@example.com"));

        var result = await _service.GetByEmailAsync("ALICE@EXAMPLE.COM");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistingEmail_ShouldReturnNull()
    {
        var result = await _service.GetByEmailAsync("nobody@example.com");

        result.Should().BeNull();
    }

    // ==================== GetAllAsync ====================

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUsers()
    {
        await SeedUserAsync(CreateUser(firstName: "Alice"));
        await SeedUserAsync(CreateUser(firstName: "Bob"));

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnPagedResults()
    {
        for (int i = 0; i < 5; i++)
            await SeedUserAsync(CreateUser(firstName: $"User{i}"));

        var result = await _service.GetAllAsync(page: 1, pageSize: 2);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_SecondPage_ShouldReturnNextSet()
    {
        for (int i = 0; i < 5; i++)
            await SeedUserAsync(CreateUser(firstName: $"User{i}"));

        var page1 = await _service.GetAllAsync(page: 1, pageSize: 2);
        var page2 = await _service.GetAllAsync(page: 2, pageSize: 2);

        page1.Should().HaveCount(2);
        page2.Should().HaveCount(2);
        page1.Select(u => u.Id).Should().NotIntersectWith(page2.Select(u => u.Id));
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ShouldReturnEmptyList()
    {
        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    // ==================== GetTotalCountAsync ====================

    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnTotalCount()
    {
        await SeedUserAsync();
        await SeedUserAsync();
        await SeedUserAsync();

        var count = await _service.GetTotalCountAsync();

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetTotalCountAsync_EmptyDatabase_ShouldReturnZero()
    {
        var count = await _service.GetTotalCountAsync();

        count.Should().Be(0);
    }

    // ==================== CreateAsync ====================

    [Fact]
    public async Task CreateAsync_ShouldCreateUser()
    {
        var user = CreateUser(firstName: "Alice", email: "ALICE@EXAMPLE.COM");

        var result = await _service.CreateAsync(user);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task CreateAsync_ShouldLowercaseEmail()
    {
        var user = CreateUser(email: "Alice@EXAMPLE.COM");

        var result = await _service.CreateAsync(user);

        result.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task CreateAsync_ShouldAssignNewGuidId()
    {
        var user = CreateUser();
        user.Id = "old-id";

        var result = await _service.CreateAsync(user);

        result.Id.Should().NotBe("old-id");
        Guid.TryParse(result.Id, out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldSetTimestamps()
    {
        var user = CreateUser();

        var result = await _service.CreateAsync(user);

        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAsync_ShouldPublishUserCreatedEvent()
    {
        var user = CreateUser(firstName: "Alice", lastName: "Smith", email: "alice@example.com");
        user.Roles = new List<Role> { Role.User, Role.Admin };

        await _service.CreateAsync(user);

        _userCreatedProducer.Verify(p => p.Produce(
            It.Is<UserCreated>(e =>
                e.Email == "alice@example.com" &&
                e.FirstName == "Alice" &&
                e.LastName == "Smith" &&
                e.Role.Contains("User") &&
                e.Role.Contains("Admin")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistInDatabase()
    {
        var user = CreateUser(firstName: "Persisted");

        var result = await _service.CreateAsync(user);

        var fromDb = await _dbContext.Users.FindAsync(result.Id);
        fromDb.Should().NotBeNull();
        fromDb!.FirstName.Should().Be("Persisted");
    }

    // ==================== UpdateAsync ====================

    [Fact]
    public async Task UpdateAsync_WithExistingUser_ShouldReturnTrue()
    {
        await SeedUserAsync(CreateUser(id: "user-1", firstName: "Old"));

        var updated = CreateUser(firstName: "New", lastName: "Name", email: "new@example.com");
        var result = await _service.UpdateAsync("user-1", updated);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAllFields()
    {
        await SeedUserAsync(CreateUser(id: "user-1", firstName: "Old", email: "old@example.com"));

        var updated = CreateUser(firstName: "New", lastName: "Name", email: "new@example.com");
        updated.Roles = new List<Role> { Role.Admin };
        updated.IsActive = false;
        updated.EmailConfirmed = true;
        await _service.UpdateAsync("user-1", updated);

        var fromDb = await _dbContext.Users.FindAsync("user-1");
        fromDb!.FirstName.Should().Be("New");
        fromDb.LastName.Should().Be("Name");
        fromDb.Email.Should().Be("new@example.com");
        fromDb.Roles.Should().Contain(Role.Admin);
        fromDb.IsActive.Should().BeFalse();
        fromDb.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTimestamp()
    {
        var oldTime = DateTime.UtcNow.AddDays(-1);
        var user = CreateUser(id: "user-1");
        user.UpdatedAt = oldTime;
        await SeedUserAsync(user);

        await _service.UpdateAsync("user-1", CreateUser());

        var fromDb = await _dbContext.Users.FindAsync("user-1");
        fromDb!.UpdatedAt.Should().BeAfter(oldTime);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPublishUserUpdatedEvent()
    {
        await SeedUserAsync(CreateUser(id: "user-1"));

        var updated = CreateUser(firstName: "Updated", email: "updated@example.com");
        await _service.UpdateAsync("user-1", updated);

        _userUpdatedProducer.Verify(p => p.Produce(
            It.Is<UserUpdated>(e =>
                e.UserId == "user-1" &&
                e.FirstName == "Updated" &&
                e.Email == "updated@example.com"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingUser_ShouldReturnFalse()
    {
        var result = await _service.UpdateAsync("nonexistent", CreateUser());

        result.Should().BeFalse();
    }

    // ==================== DeleteAsync ====================

    [Fact]
    public async Task DeleteAsync_WithExistingUser_ShouldDeleteAndReturnTrue()
    {
        await SeedUserAsync(CreateUser(id: "user-1"));

        var result = await _service.DeleteAsync("user-1");

        result.Should().BeTrue();
        var fromDb = await _dbContext.Users.FindAsync("user-1");
        fromDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldPublishUserDeletedEvent()
    {
        await SeedUserAsync(CreateUser(id: "user-1"));

        await _service.DeleteAsync("user-1");

        _userDeletedProducer.Verify(p => p.Produce(
            It.Is<UserDeleted>(e => e.UserId == "user-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingUser_ShouldReturnFalse()
    {
        var result = await _service.DeleteAsync("nonexistent");

        result.Should().BeFalse();
    }

    // ==================== UpdateLastLoginAsync ====================

    [Fact]
    public async Task UpdateLastLoginAsync_WithExistingUser_ShouldUpdateLastLogin()
    {
        await SeedUserAsync(CreateUser(id: "user-1"));

        var result = await _service.UpdateLastLoginAsync("user-1");

        result.Should().BeTrue();
        var fromDb = await _dbContext.Users.FindAsync("user-1");
        fromDb!.LastLoginAt.Should().NotBeNull();
        fromDb.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateLastLoginAsync_ShouldAlsoUpdateUpdatedAt()
    {
        var oldTime = DateTime.UtcNow.AddDays(-1);
        var user = CreateUser(id: "user-1");
        user.UpdatedAt = oldTime;
        await SeedUserAsync(user);

        await _service.UpdateLastLoginAsync("user-1");

        var fromDb = await _dbContext.Users.FindAsync("user-1");
        fromDb!.UpdatedAt.Should().BeAfter(oldTime);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_WithNonExistingUser_ShouldReturnFalse()
    {
        var result = await _service.UpdateLastLoginAsync("nonexistent");

        result.Should().BeFalse();
    }

    // ==================== DeactivateUserAsync ====================

    [Fact]
    public async Task DeactivateUserAsync_WithExistingUser_ShouldDeactivateAndReturnTrue()
    {
        await SeedUserAsync(CreateUser(id: "user-1", isActive: true));

        var result = await _service.DeactivateUserAsync("user-1");

        result.Should().BeTrue();
        var fromDb = await _dbContext.Users.FindAsync("user-1");
        fromDb!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateUserAsync_ShouldPublishUserDeactivatedEvent()
    {
        await SeedUserAsync(CreateUser(id: "user-1"));

        await _service.DeactivateUserAsync("user-1");

        _userDeactivatedProducer.Verify(p => p.Produce(
            It.Is<UserDeactivated>(e => e.UserId == "user-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateUserAsync_ShouldUpdateTimestamp()
    {
        var oldTime = DateTime.UtcNow.AddDays(-1);
        var user = CreateUser(id: "user-1");
        user.UpdatedAt = oldTime;
        await SeedUserAsync(user);

        await _service.DeactivateUserAsync("user-1");

        var fromDb = await _dbContext.Users.FindAsync("user-1");
        fromDb!.UpdatedAt.Should().BeAfter(oldTime);
    }

    [Fact]
    public async Task DeactivateUserAsync_WithNonExistingUser_ShouldReturnFalse()
    {
        var result = await _service.DeactivateUserAsync("nonexistent");

        result.Should().BeFalse();
    }

    // ==================== EmailExistsAsync ====================

    [Fact]
    public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
    {
        await SeedUserAsync(CreateUser(email: "alice@example.com"));

        var result = await _service.EmailExistsAsync("alice@example.com");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_CaseInsensitive_ShouldReturnTrue()
    {
        await SeedUserAsync(CreateUser(email: "alice@example.com"));

        var result = await _service.EmailExistsAsync("ALICE@EXAMPLE.COM");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_WithNonExistingEmail_ShouldReturnFalse()
    {
        var result = await _service.EmailExistsAsync("nobody@example.com");

        result.Should().BeFalse();
    }

    // ==================== MapToResponse ====================

    [Fact]
    public void MapToResponse_ShouldMapAllFields()
    {
        var user = new User
        {
            Id = "user-1",
            Email = "alice@example.com",
            FirstName = "Alice",
            LastName = "Smith",
            Roles = new List<Role> { Role.User, Role.Admin },
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastLoginAt = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = _service.MapToResponse(user);

        result.Should().NotBeNull();
        result.Id.Should().Be("user-1");
        result.Email.Should().Be("alice@example.com");
        result.FirstName.Should().Be("Alice");
        result.LastName.Should().Be("Smith");
        result.Roles.Should().HaveCount(2);
        result.Roles.Should().Contain(new[] { Role.User, Role.Admin });
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.LastLoginAt.Should().Be(new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void MapToResponse_WithNullLastLogin_ShouldMapCorrectly()
    {
        var user = new User
        {
            Id = "user-1",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            LastLoginAt = null
        };

        var result = _service.MapToResponse(user);

        result.LastLoginAt.Should().BeNull();
    }
}
