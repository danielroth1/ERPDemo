using MongoDB.Driver;
using SalesManagement.Infrastructure;
using SalesManagement.Models;
using SalesManagement.Models.DTOs;

namespace SalesManagement.Services;

public interface IOrderService
{
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request);
    Task<OrderResponse?> GetOrderByIdAsync(string id);
    Task<List<OrderResponse>> GetAllOrdersAsync(int skip = 0, int limit = 100);
    Task<List<OrderResponse>> GetOrdersByCustomerAsync(string customerId, int skip = 0, int limit = 100);
    Task<List<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status, int skip = 0, int limit = 100);
    Task<OrderResponse?> UpdateOrderAsync(string id, UpdateOrderRequest request);
    Task<OrderResponse?> UpdateOrderStatusAsync(string id, OrderStatus status);
    Task<bool> DeleteOrderAsync(string id);
}

public class OrderService : IOrderService
{
    private readonly MongoDbContext _context;
    private readonly ICustomerService _customerService;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<OrderService> _logger;
    private const string OrderTopic = "sales-events";
    private const decimal TaxRate = 0.1m; // 10% tax

    public OrderService(
        MongoDbContext context,
        ICustomerService customerService,
        KafkaProducer kafkaProducer,
        ILogger<OrderService> logger)
    {
        _context = context;
        _customerService = customerService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        // Validate customer exists
        var customer = await _customerService.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null)
        {
            throw new ArgumentException($"Customer with ID {request.CustomerId} not found");
        }

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync();

        // Calculate totals
        var items = new List<OrderItem>();
        decimal subtotal = 0;

        foreach (var itemRequest in request.Items)
        {
            // In a real scenario, fetch product details from inventory service
            var item = new OrderItem
            {
                ProductId = itemRequest.ProductId,
                ProductName = $"Product {itemRequest.ProductId}", // Placeholder
                Sku = $"SKU-{itemRequest.ProductId}", // Placeholder
                Quantity = itemRequest.Quantity,
                UnitPrice = 100m, // Placeholder - should come from inventory
                Discount = itemRequest.Discount,
                Subtotal = itemRequest.Quantity * 100m,
                Total = (itemRequest.Quantity * 100m) - itemRequest.Discount
            };

            items.Add(item);
            subtotal += item.Total;
        }

        var tax = subtotal * TaxRate;
        var total = subtotal + tax - request.Discount;

        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderNumber = orderNumber,
            Items = items,
            Subtotal = subtotal,
            Tax = tax,
            Discount = request.Discount,
            Total = total,
            Status = OrderStatus.Pending,
            Notes = request.Notes,
            ShippingAddress = request.ShippingAddress != null ? MapAddress(request.ShippingAddress) : customer.DefaultShippingAddress,
            BillingAddress = request.BillingAddress != null ? MapAddress(request.BillingAddress) : customer.DefaultBillingAddress
        };

        await _context.Orders.InsertOneAsync(order);

        _logger.LogInformation("Created order {OrderNumber} for customer {CustomerId}", orderNumber, request.CustomerId);

        // Publish event
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            OrderNumber = order.OrderNumber,
            Total = order.Total,
            Items = order.Items.Select(i => new OrderItemEvent
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            CreatedAt = order.CreatedAt
        };

        await _kafkaProducer.PublishAsync(OrderTopic, order.Id, orderEvent);

        return MapToResponse(order);
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(string id)
    {
        var order = await _context.Orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        return order != null ? MapToResponse(order) : null;
    }

    public async Task<List<OrderResponse>> GetAllOrdersAsync(int skip = 0, int limit = 100)
    {
        var orders = await _context.Orders
            .Find(_ => true)
            .SortByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return orders.Select(MapToResponse).ToList();
    }

    public async Task<List<OrderResponse>> GetOrdersByCustomerAsync(string customerId, int skip = 0, int limit = 100)
    {
        var orders = await _context.Orders
            .Find(o => o.CustomerId == customerId)
            .SortByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return orders.Select(MapToResponse).ToList();
    }

    public async Task<List<OrderResponse>> GetOrdersByStatusAsync(OrderStatus status, int skip = 0, int limit = 100)
    {
        var orders = await _context.Orders
            .Find(o => o.Status == status)
            .SortByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return orders.Select(MapToResponse).ToList();
    }

    public async Task<OrderResponse?> UpdateOrderAsync(string id, UpdateOrderRequest request)
    {
        var order = await _context.Orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        if (order == null) return null;

        // Can only update draft or pending orders
        if (order.Status != OrderStatus.Draft && order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot update order in {order.Status} status");
        }

        if (request.Items != null)
        {
            var items = new List<OrderItem>();
            decimal subtotal = 0;

            foreach (var itemRequest in request.Items)
            {
                var item = new OrderItem
                {
                    ProductId = itemRequest.ProductId,
                    ProductName = $"Product {itemRequest.ProductId}",
                    Sku = $"SKU-{itemRequest.ProductId}",
                    Quantity = itemRequest.Quantity,
                    UnitPrice = 100m,
                    Discount = itemRequest.Discount,
                    Subtotal = itemRequest.Quantity * 100m,
                    Total = (itemRequest.Quantity * 100m) - itemRequest.Discount
                };

                items.Add(item);
                subtotal += item.Total;
            }

            var tax = subtotal * TaxRate;
            var total = subtotal + tax - (request.Discount ?? order.Discount);

            order.Items = items;
            order.Subtotal = subtotal;
            order.Tax = tax;
            order.Total = total;
        }

        if (request.Discount.HasValue)
        {
            order.Discount = request.Discount.Value;
            order.Total = order.Subtotal + order.Tax - order.Discount;
        }

        if (request.Notes != null) order.Notes = request.Notes;
        if (request.ShippingAddress != null) order.ShippingAddress = MapAddress(request.ShippingAddress);
        if (request.BillingAddress != null) order.BillingAddress = MapAddress(request.BillingAddress);

        order.UpdatedAt = DateTime.UtcNow;

        await _context.Orders.ReplaceOneAsync(o => o.Id == id, order);

        _logger.LogInformation("Updated order {OrderNumber}", order.OrderNumber);

        return MapToResponse(order);
    }

    public async Task<OrderResponse?> UpdateOrderStatusAsync(string id, OrderStatus status)
    {
        var order = await _context.Orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        if (order == null) return null;

        var oldStatus = order.Status;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Completed)
        {
            order.CompletedAt = DateTime.UtcNow;
        }
        else if (status == OrderStatus.Cancelled)
        {
            order.CancelledAt = DateTime.UtcNow;
        }

        await _context.Orders.ReplaceOneAsync(o => o.Id == id, order);

        _logger.LogInformation("Updated order {OrderNumber} status from {OldStatus} to {NewStatus}", 
            order.OrderNumber, oldStatus, status);

        // Publish status change event
        var statusEvent = new OrderStatusChangedEvent
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            OldStatus = oldStatus.ToString(),
            NewStatus = status.ToString(),
            ChangedAt = DateTime.UtcNow
        };

        await _kafkaProducer.PublishAsync(OrderTopic, order.Id, statusEvent);

        return MapToResponse(order);
    }

    public async Task<bool> DeleteOrderAsync(string id)
    {
        var order = await _context.Orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        if (order == null) return false;

        // Can only delete draft orders
        if (order.Status != OrderStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot delete order in {order.Status} status");
        }

        var result = await _context.Orders.DeleteOneAsync(o => o.Id == id);

        _logger.LogInformation("Deleted order {OrderNumber}", order.OrderNumber);

        return result.DeletedCount > 0;
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var count = await _context.Orders.CountDocumentsAsync(_ => true);
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D6}";
    }

    private static Address MapAddress(AddressRequest request)
    {
        return new Address
        {
            Street = request.Street,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country
        };
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderNumber = order.OrderNumber,
            Items = order.Items,
            Subtotal = order.Subtotal,
            Tax = order.Tax,
            Discount = order.Discount,
            Total = order.Total,
            Status = order.Status.ToString(),
            Notes = order.Notes,
            ShippingAddress = order.ShippingAddress,
            BillingAddress = order.BillingAddress,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CompletedAt = order.CompletedAt,
            CancelledAt = order.CancelledAt
        };
    }
}
