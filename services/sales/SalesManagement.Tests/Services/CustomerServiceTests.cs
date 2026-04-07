using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SalesManagement.Infrastructure;
using SalesManagement.Models;
using SalesManagement.Models.DTOs;
using SalesManagement.Services;
using SalesManagement.Tests.Helpers;

namespace SalesManagement.Tests.Services;

public class CustomerServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<CustomerService>> _loggerMock;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _loggerMock = new Mock<ILogger<CustomerService>>();
        _service = new CustomerService(_dbContext, _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private static CreateCustomerRequest CreateValidRequest(string? email = null) => new()
    {
        FirstName = "John",
        LastName = "Doe",
        Email = email ?? $"john.doe.{Guid.NewGuid():N}@example.com",
        Phone = "+1234567890",
        Company = "Acme Inc"
    };

    [Fact]
    public async Task CreateCustomerAsync_WithValidRequest_ShouldCreateCustomer()
    {
        // Arrange
        var request = CreateValidRequest();

        // Act
        var result = await _service.CreateCustomerAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be(request.Email);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCustomerAsync_WithDuplicateEmail_ShouldThrowException()
    {
        // Arrange
        var request = CreateValidRequest("duplicate@example.com");
        await _service.CreateCustomerAsync(request);

        // Act
        var act = () => _service.CreateCustomerAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithExistingId_ShouldReturnCustomer()
    {
        // Arrange
        var created = await _service.CreateCustomerAsync(CreateValidRequest());

        // Act
        var result = await _service.GetCustomerByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetCustomerByIdAsync("non-existing-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCustomerByEmailAsync_WithExistingEmail_ShouldReturnCustomer()
    {
        // Arrange
        var request = CreateValidRequest("findme@example.com");
        await _service.CreateCustomerAsync(request);

        // Act
        var result = await _service.GetCustomerByEmailAsync("findme@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("findme@example.com");
    }

    [Fact]
    public async Task GetAllCustomersAsync_ShouldReturnOnlyActiveCustomers()
    {
        // Arrange
        await _service.CreateCustomerAsync(CreateValidRequest());
        await _service.CreateCustomerAsync(CreateValidRequest());

        // Add an inactive customer directly
        _dbContext.Customers.Add(new Customer
        {
            FirstName = "Inactive",
            LastName = "User",
            Email = "inactive@example.com",
            IsActive = false
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAllCustomersAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public async Task SearchCustomersAsync_ShouldReturnMatchingCustomers()
    {
        // Arrange
        var aliceRequest = CreateValidRequest("alice@example.com");
        aliceRequest.FirstName = "Alice";
        await _service.CreateCustomerAsync(aliceRequest);

        var bobRequest = CreateValidRequest("bob@example.com");
        bobRequest.FirstName = "Bob";
        await _service.CreateCustomerAsync(bobRequest);

        // Act
        var result = await _service.SearchCustomersAsync("Alice");

        // Assert
        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("Alice");
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithValidData_ShouldUpdateCustomer()
    {
        // Arrange
        var created = await _service.CreateCustomerAsync(CreateValidRequest());
        var updateRequest = new UpdateCustomerRequest { FirstName = "Jane", Phone = "+9876543210" };

        // Act
        var result = await _service.UpdateCustomerAsync(created.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Jane");
        result.Phone.Should().Be("+9876543210");
    }

    [Fact]
    public async Task UpdateCustomerAsync_WithDuplicateEmail_ShouldThrowException()
    {
        // Arrange
        await _service.CreateCustomerAsync(CreateValidRequest("first@example.com"));
        var second = await _service.CreateCustomerAsync(CreateValidRequest("second@example.com"));
        var updateRequest = new UpdateCustomerRequest { Email = "first@example.com" };

        // Act
        var act = () => _service.UpdateCustomerAsync(second.Id, updateRequest);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task DeleteCustomerAsync_ShouldSoftDelete()
    {
        // Arrange
        var created = await _service.CreateCustomerAsync(CreateValidRequest());

        // Act
        var result = await _service.DeleteCustomerAsync(created.Id);

        // Assert
        result.Should().BeTrue();
        var customer = await _dbContext.Customers.FindAsync(created.Id);
        customer.Should().NotBeNull();
        customer!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCustomerAsync_WithNonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteCustomerAsync("non-existing-id");

        // Assert
        result.Should().BeFalse();
    }
}
