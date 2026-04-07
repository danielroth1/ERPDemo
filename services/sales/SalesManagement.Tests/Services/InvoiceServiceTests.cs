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

public class InvoiceServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<ITopicProducer<InvoiceCreated>> _invoiceCreatedProducerMock;
    private readonly Mock<ITopicProducer<InvoicePaid>> _invoicePaidProducerMock;
    private readonly Mock<ILogger<InvoiceService>> _loggerMock;
    private readonly InvoiceService _service;

    public InvoiceServiceTests()
    {
        _dbContext = DbContextHelper.CreateInMemoryContext();
        _orderServiceMock = new Mock<IOrderService>();
        _invoiceCreatedProducerMock = new Mock<ITopicProducer<InvoiceCreated>>();
        _invoicePaidProducerMock = new Mock<ITopicProducer<InvoicePaid>>();
        _loggerMock = new Mock<ILogger<InvoiceService>>();

        _service = new InvoiceService(
            _dbContext,
            _orderServiceMock.Object,
            _invoiceCreatedProducerMock.Object,
            _invoicePaidProducerMock.Object,
            _loggerMock.Object);
    }

    public void Dispose() => _dbContext.Dispose();

    private void SetupOrderExists(string orderId = "order-1")
    {
        _orderServiceMock
            .Setup(o => o.GetOrderByIdAsync(orderId))
            .ReturnsAsync(new OrderResponse
            {
                Id = orderId,
                CustomerId = "customer-1",
                OrderNumber = "ORD-20260101-000001",
                Items = new List<OrderItem>
                {
                    new() { ProductId = "prod-1", ProductName = "Widget", Quantity = 2, UnitPrice = 50, Subtotal = 100, Total = 100 }
                },
                Subtotal = 100,
                Tax = 10,
                Discount = 0,
                Total = 110,
                Status = OrderStatus.Confirmed.ToString()
            });
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithValidRequest_ShouldCreateInvoice()
    {
        // Arrange
        SetupOrderExists();
        var request = new CreateInvoiceRequest { OrderId = "order-1" };

        // Act
        var result = await _service.CreateInvoiceAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.InvoiceNumber.Should().StartWith("INV-");
        result.OrderId.Should().Be("order-1");
        result.CustomerId.Should().Be("customer-1");
        result.Total.Should().Be(110);
        result.AmountDue.Should().Be(110);
        result.AmountPaid.Should().Be(0);
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldPublishInvoiceCreatedEvent()
    {
        // Arrange
        SetupOrderExists();
        var request = new CreateInvoiceRequest { OrderId = "order-1" };

        // Act
        await _service.CreateInvoiceAsync(request);

        // Assert
        _invoiceCreatedProducerMock.Verify(
            p => p.Produce(It.IsAny<InvoiceCreated>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithNonExistingOrder_ShouldThrowException()
    {
        // Arrange
        _orderServiceMock
            .Setup(o => o.GetOrderByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((OrderResponse?)null);

        var request = new CreateInvoiceRequest { OrderId = "non-existing" };

        // Act
        var act = () => _service.CreateInvoiceAsync(request);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithDuplicateOrder_ShouldThrowException()
    {
        // Arrange
        SetupOrderExists();
        await _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-1" });

        // Act
        var act = () => _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-1" });

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task CreateInvoiceAsync_WithCustomDueDate_ShouldUseThatDate()
    {
        // Arrange
        SetupOrderExists();
        var dueDate = DateTime.UtcNow.AddDays(60);
        var request = new CreateInvoiceRequest { OrderId = "order-1", DueDate = dueDate };

        // Act
        var result = await _service.CreateInvoiceAsync(request);

        // Assert
        result.DueDate.Should().BeCloseTo(dueDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetInvoiceByIdAsync_WithExistingId_ShouldReturnInvoice()
    {
        // Arrange
        SetupOrderExists();
        var created = await _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-1" });

        // Act
        var result = await _service.GetInvoiceByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetInvoiceByOrderIdAsync_ShouldReturnCorrectInvoice()
    {
        // Arrange
        SetupOrderExists();
        await _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-1" });

        // Act
        var result = await _service.GetInvoiceByOrderIdAsync("order-1");

        // Assert
        result.Should().NotBeNull();
        result!.OrderId.Should().Be("order-1");
    }

    [Fact]
    public async Task RecordPaymentAsync_FullPayment_ShouldMarkAsPaid()
    {
        // Arrange
        SetupOrderExists();
        var invoice = await _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-1" });
        var paymentRequest = new RecordPaymentRequest { Amount = 110 };

        // Act
        var result = await _service.RecordPaymentAsync(invoice.Id, paymentRequest);

        // Assert
        result.Should().NotBeNull();
        result!.AmountPaid.Should().Be(110);
        result.AmountDue.Should().Be(0);
        result.Status.Should().Be(InvoiceStatus.Paid.ToString());
        result.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RecordPaymentAsync_PartialPayment_ShouldMarkAsPartiallyPaid()
    {
        // Arrange
        SetupOrderExists();
        var invoice = await _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-1" });
        var paymentRequest = new RecordPaymentRequest { Amount = 50 };

        // Act
        var result = await _service.RecordPaymentAsync(invoice.Id, paymentRequest);

        // Assert
        result.Should().NotBeNull();
        result!.AmountPaid.Should().Be(50);
        result.AmountDue.Should().Be(60);
        result.Status.Should().Be(InvoiceStatus.PartiallyPaid.ToString());
    }

    [Fact]
    public async Task RecordPaymentAsync_FullPayment_ShouldPublishInvoicePaidEvent()
    {
        // Arrange
        SetupOrderExists();
        var invoice = await _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-1" });
        var paymentRequest = new RecordPaymentRequest { Amount = 110 };

        // Act
        await _service.RecordPaymentAsync(invoice.Id, paymentRequest);

        // Assert
        _invoicePaidProducerMock.Verify(
            p => p.Produce(It.IsAny<InvoicePaid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteInvoiceAsync_DraftInvoice_ShouldReturnTrue()
    {
        // Arrange — seed a Draft invoice directly (service creates Pending invoices)
        var invoice = new Invoice
        {
            Id = Guid.NewGuid().ToString(),
            OrderId = "order-1",
            CustomerId = "customer-1",
            InvoiceNumber = "INV-TEST-000001",
            Status = InvoiceStatus.Draft,
            Items = new List<OrderItem>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.DeleteInvoiceAsync(invoice.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllInvoicesAsync_ShouldReturnAllInvoices()
    {
        // Arrange
        SetupOrderExists("order-1");
        _orderServiceMock
            .Setup(o => o.GetOrderByIdAsync("order-2"))
            .ReturnsAsync(new OrderResponse
            {
                Id = "order-2",
                CustomerId = "customer-2",
                OrderNumber = "ORD-20260101-000002",
                Items = new List<OrderItem>(),
                Subtotal = 200,
                Tax = 20,
                Discount = 0,
                Total = 220,
                Status = OrderStatus.Confirmed.ToString()
            });

        await _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-1" });
        await _service.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = "order-2" });

        // Act
        var result = await _service.GetAllInvoicesAsync();

        // Assert
        result.Should().HaveCount(2);
    }
}
