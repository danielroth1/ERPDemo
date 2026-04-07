using FluentAssertions;
using SalesManagement.Models;

namespace SalesManagement.Tests.Models;

public class OrderModelTests
{
    [Fact]
    public void Order_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var order = new Order();

        // Assert
        order.Id.Should().NotBeNullOrEmpty();
        order.Status.Should().Be(OrderStatus.Draft);
        order.Items.Should().BeEmpty();
        order.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void OrderStatus_AllValues_ShouldBeValid(OrderStatus status)
    {
        // Act
        var order = new Order { Status = status };

        // Assert
        order.Status.Should().Be(status);
        Enum.IsDefined(typeof(OrderStatus), status).Should().BeTrue();
    }

    [Fact]
    public void Customer_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var customer = new Customer();

        // Assert
        customer.Id.Should().NotBeNullOrEmpty();
        customer.IsActive.Should().BeTrue();
        customer.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Invoice_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var invoice = new Invoice();

        // Assert
        invoice.Id.Should().NotBeNullOrEmpty();
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.Items.Should().BeEmpty();
        invoice.AmountPaid.Should().Be(0);
    }

    [Theory]
    [InlineData(InvoiceStatus.Draft)]
    [InlineData(InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.Sent)]
    [InlineData(InvoiceStatus.PartiallyPaid)]
    [InlineData(InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.Cancelled)]
    public void InvoiceStatus_AllValues_ShouldBeValid(InvoiceStatus status)
    {
        // Act
        var invoice = new Invoice { Status = status };

        // Assert
        invoice.Status.Should().Be(status);
    }

    [Fact]
    public void OrderItem_ShouldHoldValues()
    {
        // Act
        var item = new OrderItem
        {
            ProductId = "prod-1",
            ProductName = "Widget",
            Sku = "WDG-001",
            Quantity = 5,
            UnitPrice = 19.99m,
            Discount = 2.00m,
            Subtotal = 99.95m,
            Total = 97.95m
        };

        // Assert
        item.ProductId.Should().Be("prod-1");
        item.Quantity.Should().Be(5);
        item.UnitPrice.Should().Be(19.99m);
    }

    [Fact]
    public void Address_ShouldHoldValues()
    {
        // Act
        var address = new Address
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            PostalCode = "62701",
            Country = "US"
        };

        // Assert
        address.Street.Should().Be("123 Main St");
        address.Country.Should().Be("US");
    }
}
