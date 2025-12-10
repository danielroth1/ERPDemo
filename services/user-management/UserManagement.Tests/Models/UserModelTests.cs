using FluentAssertions;
using UserManagement.Models;
using Xunit;

namespace UserManagement.Tests.Models;

public class UserModelTests
{
    [Fact]
    public void User_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var user = new User();

        // Assert
        user.Id.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
        user.FirstName.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.IsActive.Should().BeTrue();
        user.Roles.Should().ContainSingle();
        user.Roles.Should().Contain(Role.User);
    }

    [Fact]
    public void User_SetProperties_ShouldWork()
    {
        // Arrange & Act
        var user = new User
        {
            Id = "507f1f77bcf86cd799439011",
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true,
            Roles = new List<Role> { Role.Admin, Role.Manager }
        };

        // Assert
        user.Id.Should().Be("507f1f77bcf86cd799439011");
        user.Email.Should().Be("test@example.com");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.IsActive.Should().BeTrue();
        user.Roles.Should().HaveCount(2);
        user.Roles.Should().Contain(new[] { Role.Admin, Role.Manager });
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("first.last+tag@example.com")]
    public void User_Email_ShouldAcceptValidFormats(string email)
    {
        // Arrange & Act
        var user = new User { Email = email };

        // Assert
        user.Email.Should().Be(email);
    }

    [Fact]
    public void User_FullName_ShouldCombineFirstAndLast()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = $"{user.FirstName} {user.LastName}";

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void User_MultipleRoles_ShouldBeSupported()
    {
        // Arrange & Act
        var user = new User
        {
            Roles = new List<Role> { Role.User, Role.Manager, Role.Admin }
        };

        // Assert
        user.Roles.Should().HaveCount(3);
        user.Roles.Should().Contain(Role.User);
        user.Roles.Should().Contain(Role.Manager);
        user.Roles.Should().Contain(Role.Admin);
    }
}
