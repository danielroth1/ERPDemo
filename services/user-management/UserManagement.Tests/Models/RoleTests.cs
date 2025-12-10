using FluentAssertions;
using UserManagement.Models;
using Xunit;

namespace UserManagement.Tests.Models;

public class RoleTests
{
    [Theory]
    [InlineData(Role.Admin)]
    [InlineData(Role.Manager)]
    [InlineData(Role.User)]
    public void Role_AllValues_ShouldBeValid(Role role)
    {
        // Assert
        role.Should().BeOneOf(Role.Admin, Role.Manager, Role.User);
    }

    [Fact]
    public void Role_Admin_ShouldHaveCorrectValue()
    {
        // Arrange & Act
        var role = Role.Admin;

        // Assert
        role.Should().Be(Role.Admin);
    }

    [Fact]
    public void Role_Manager_ShouldHaveCorrectValue()
    {
        // Arrange & Act
        var role = Role.Manager;

        // Assert
        role.Should().Be(Role.Manager);
    }

    [Fact]
    public void Role_User_ShouldHaveCorrectValue()
    {
        // Arrange & Act
        var role = Role.User;

        // Assert
        role.Should().Be(Role.User);
    }

    [Fact]
    public void Role_List_ShouldSupportMultipleRoles()
    {
        // Arrange & Act
        var roles = new List<Role> { Role.User, Role.Manager };

        // Assert
        roles.Should().HaveCount(2);
        roles.Should().Contain(Role.User);
        roles.Should().Contain(Role.Manager);
    }
}
