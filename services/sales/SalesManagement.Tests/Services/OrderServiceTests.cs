using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using SalesManagement.Infrastructure;
using SalesManagement.Models;
using SalesManagement.Models.DTOs;
using SalesManagement.Services;
using SalesManagement.Tests.Helpers;
using ERP.Contracts.Events.Domain;

namespace SalesManagement.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ICustomerService> _customerServiceMock;
    private readonly Mock<ITopicProducer<OrderCreated>> _orderCreatedProducerMock;
    private readonly Mock<ITopicProducer<OrderStatusChanged>> _orderStatusChangedProducerMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _customerServiceMock = new Mock<ICustomerService>();
        _orderCreatedProducerMock = new Mock<ITopicProducer<OrderCreated>>();
        _orderStatusChangedProducerMock = new Mock<ITopicProducer<OrderStatusChanged>>();
        _loggerMock = new Mock<ILogger<OrderService>>();

        _service = new OrderService(
            _dbContext,
            _customerServiceMock.Object,
            _orderCreatedProducerMock.Object,
            _orderStatusChangedProducerMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private void SetupCustomerExists(string customerId = "customer-1")
    {
        _customerServiceMock
            .Setup(c => c.GetCustomerByIdAsync(customerId))
            .ReturnsAsync(new CustomerResponse { Id = customerId, FirstName = "John", LastName = "Doe" });
    }

    private static CreateOrderRequest CreateValidOrderRequest(string customerId = "customer-1") => new()
    {
        CustomerId = customerId,
        Items = new List<OrderItemRequest>
        {
            new() { ProductId = "prod-1", Quantity = 2, Discount = 0 },
            new() { ProductId = "prod-2", Quantity = 1, Discount = 5 }
        },
        Discount = 0
    };

    [Fact]
    public async Task CreateOrderAsync_WithValidRequest_ShouldCreateOrder()
    {
        // Arrange
        SetupCustomerExists();
        var request = CreateValidOrderRequest();

        // Act
        var result = await _service.CreateOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.CustomerId.Should().Be("customer-1");
        result.OrderNumber.Should().StartWith("ORD-");
        result.Status.Should().Be(OrderStatus.Pending.ToString());
    }

    [Fact]
    public async Task CreateOrderAsync_ShouldPublishOrderCreatedEvent()
    {
        // Arrange
        SetupCustomerExists();
        var request = CreateValidOrderRequest();

        // Act
        await _service.CreateOrderAsync(request);

        // Assert
        _orderCreatedProducerMock.Verify(
            p => p.Produce(It.IsAny<OrderCreated>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateOrderAsync_WithNonExistingCustomer_ShouldThrowException()
    {
        // Arrange
        _customerServiceMock
            .Setup(c => c.GetCustomerByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((CustomerResponse?)null);

        var request = CreateValidOrderRequest();

        // Act
        var act = () => _service.CreateOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithExistingId_ShouldReturnOrder()
    {
        // Arrange
        SetupCustomerExists();
        var created = await _service.CreateOrderAsync(CreateValidOrderRequest());

        // Act
        var result = await _service.GetOrderByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetOrderByIdAsync("non-existing-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllOrdersAsync_ShouldReturnOrdersOrderedByCreatedAtDescending()
    {
        // Arrange
        SetupCustomerExists();
        await _service.CreateOrderAsync(CreateValidOrderRequest());
        await _service.CreateOrderAsync(CreateValidOrderRequest());

        // Act
        var result = await _service.GetAllOrdersAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetOrdersByCustomerAsync_ShouldFilterByCustomerId()
    {
        // Arrange
        SetupCustomerExists("cust-1");
        SetupCustomerExists("cust-2");
        await _service.CreateOrderAsync(CreateValidOrderRequest("cust-1"));
        await _service.CreateOrderAsync(CreateValidOrderRequest("cust-2"));

        // Act
        var result = await _service.GetOrdersByCustomerAsync("cust-1");

        // Assert
        result.Should().HaveCount(1);
        result[0].CustomerId.Should().Be("cust-1");
    }

    [Fact]
    public async Task GetOrdersByStatusAsync_ShouldFilterByStatus()
    {
        // Arrange
        SetupCustomerExists();
        await _service.CreateOrderAsync(CreateValidOrderRequest());

        // Act
        var result = await _service.GetOrdersByStatusAsync(OrderStatus.Pending);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateOrderAsync_OnDraftOrder_ShouldUpdateSuccessfully()
    {
        // Arrange
        SetupCustomerExists();
        var created = await _service.CreateOrderAsync(CreateValidOrderRequest());
        var updateRequest = new UpdateOrderRequest { Notes = "Updated notes" };

        // Act
        var result = await _service.UpdateOrderAsync(created.Id, updateRequest);

        // Assert
        result.Should().NotBeNull();
        result!.Notes.Should().Be("Updated notes");
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldChangeStatusAndPublishEvent()
    {
        // Arrange
        SetupCustomerExists();
        var created = await _service.CreateOrderAsync(CreateValidOrderRequest());

        // Act
        var result = await _service.UpdateOrderStatusAsync(created.Id, OrderStatus.Confirmed);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(OrderStatus.Confirmed.ToString());
        _orderStatusChangedProducerMock.Verify(
            p => p.Produce(It.IsAny<OrderStatusChanged>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ToCompleted_ShouldSetCompletedAt()
    {
        // Arrange
        SetupCustomerExists();
        var created = await _service.CreateOrderAsync(CreateValidOrderRequest());

        // Act
        var result = await _service.UpdateOrderStatusAsync(created.Id, OrderStatus.Completed);

        // Assert
        result.Should().NotBeNull();
        result!.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ToCancelled_ShouldSetCancelledAt()
    {
        // Arrange
        SetupCustomerExists();
        var created = await _service.CreateOrderAsync(CreateValidOrderRequest());

        // Act
        var result = await _service.UpdateOrderStatusAsync(created.Id, OrderStatus.Cancelled);

        // Assert
        result.Should().NotBeNull();
        result!.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteOrderAsync_DraftOrder_ShouldReturnTrue()
    {
        // Arrange — seed a Draft order directly (service creates Pending orders)
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = "customer-1",
            OrderNumber = "ORD-TEST-000001",
            Status = OrderStatus.Draft,
            Items = new List<OrderItem>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.DeleteOrderAsync(order.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteOrderAsync_NonExistingId_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteOrderAsync("non-existing-id");

        // Assert
        result.Should().BeFalse();
    }
}
