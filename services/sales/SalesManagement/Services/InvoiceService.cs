using MongoDB.Driver;
using SalesManagement.Infrastructure;
using SalesManagement.Models;
using SalesManagement.Models.DTOs;

namespace SalesManagement.Services;

public interface IInvoiceService
{
    Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<InvoiceResponse?> GetInvoiceByIdAsync(string id);
    Task<InvoiceResponse?> GetInvoiceByOrderIdAsync(string orderId);
    Task<List<InvoiceResponse>> GetAllInvoicesAsync(int skip = 0, int limit = 100);
    Task<List<InvoiceResponse>> GetInvoicesByCustomerAsync(string customerId, int skip = 0, int limit = 100);
    Task<List<InvoiceResponse>> GetInvoicesByStatusAsync(InvoiceStatus status, int skip = 0, int limit = 100);
    Task<List<InvoiceResponse>> GetOverdueInvoicesAsync(int skip = 0, int limit = 100);
    Task<InvoiceResponse?> UpdateInvoiceAsync(string id, UpdateInvoiceRequest request);
    Task<InvoiceResponse?> RecordPaymentAsync(string id, RecordPaymentRequest request);
    Task<bool> DeleteInvoiceAsync(string id);
}

public class InvoiceService : IInvoiceService
{
    private readonly MongoDbContext _context;
    private readonly IOrderService _orderService;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<InvoiceService> _logger;
    private const string InvoiceTopic = "sales-events";

    public InvoiceService(
        MongoDbContext context,
        IOrderService orderService,
        KafkaProducer kafkaProducer,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _orderService = orderService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        // Get order details
        var order = await _orderService.GetOrderByIdAsync(request.OrderId);
        if (order == null)
        {
            throw new ArgumentException($"Order with ID {request.OrderId} not found");
        }

        // Check if invoice already exists for this order
        var existing = await _context.Invoices
            .Find(i => i.OrderId == request.OrderId)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            throw new InvalidOperationException($"Invoice already exists for order {order.OrderNumber}");
        }

        var invoiceNumber = await GenerateInvoiceNumberAsync();
        var dueDate = request.DueDate ?? DateTime.UtcNow.AddDays(30);

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            OrderId = request.OrderId,
            CustomerId = order.CustomerId,
            IssueDate = DateTime.UtcNow,
            DueDate = dueDate,
            Subtotal = order.Subtotal,
            Tax = order.Tax,
            Discount = order.Discount,
            Total = order.Total,
            AmountPaid = 0,
            AmountDue = order.Total,
            Status = InvoiceStatus.Pending,
            Items = order.Items,
            BillingAddress = order.BillingAddress,
            Notes = request.Notes
        };

        await _context.Invoices.InsertOneAsync(invoice);

        _logger.LogInformation("Created invoice {InvoiceNumber} for order {OrderNumber}", 
            invoiceNumber, order.OrderNumber);

        // Publish event
        var invoiceEvent = new InvoiceCreatedEvent
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            OrderId = invoice.OrderId,
            CustomerId = invoice.CustomerId,
            Total = invoice.Total,
            DueDate = invoice.DueDate,
            CreatedAt = invoice.CreatedAt
        };

        await _kafkaProducer.PublishAsync(InvoiceTopic, invoice.Id, invoiceEvent);

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse?> GetInvoiceByIdAsync(string id)
    {
        var invoice = await _context.Invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
        return invoice != null ? MapToResponse(invoice) : null;
    }

    public async Task<InvoiceResponse?> GetInvoiceByOrderIdAsync(string orderId)
    {
        var invoice = await _context.Invoices.Find(i => i.OrderId == orderId).FirstOrDefaultAsync();
        return invoice != null ? MapToResponse(invoice) : null;
    }

    public async Task<List<InvoiceResponse>> GetAllInvoicesAsync(int skip = 0, int limit = 100)
    {
        var invoices = await _context.Invoices
            .Find(_ => true)
            .SortByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return invoices.Select(MapToResponse).ToList();
    }

    public async Task<List<InvoiceResponse>> GetInvoicesByCustomerAsync(string customerId, int skip = 0, int limit = 100)
    {
        var invoices = await _context.Invoices
            .Find(i => i.CustomerId == customerId)
            .SortByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return invoices.Select(MapToResponse).ToList();
    }

    public async Task<List<InvoiceResponse>> GetInvoicesByStatusAsync(InvoiceStatus status, int skip = 0, int limit = 100)
    {
        var invoices = await _context.Invoices
            .Find(i => i.Status == status)
            .SortByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return invoices.Select(MapToResponse).ToList();
    }

    public async Task<List<InvoiceResponse>> GetOverdueInvoicesAsync(int skip = 0, int limit = 100)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<Invoice>.Filter.And(
            Builders<Invoice>.Filter.Lt(i => i.DueDate, now),
            Builders<Invoice>.Filter.Gt(i => i.AmountDue, 0),
            Builders<Invoice>.Filter.Ne(i => i.Status, InvoiceStatus.Paid),
            Builders<Invoice>.Filter.Ne(i => i.Status, InvoiceStatus.Cancelled)
        );

        var invoices = await _context.Invoices
            .Find(filter)
            .SortBy(i => i.DueDate)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        // Update status to overdue
        foreach (var invoice in invoices)
        {
            if (invoice.Status != InvoiceStatus.Overdue)
            {
                invoice.Status = InvoiceStatus.Overdue;
                invoice.UpdatedAt = DateTime.UtcNow;
                await _context.Invoices.ReplaceOneAsync(i => i.Id == invoice.Id, invoice);
            }
        }

        return invoices.Select(MapToResponse).ToList();
    }

    public async Task<InvoiceResponse?> UpdateInvoiceAsync(string id, UpdateInvoiceRequest request)
    {
        var invoice = await _context.Invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
        if (invoice == null) return null;

        // Can only update draft or pending invoices
        if (invoice.Status != InvoiceStatus.Draft && invoice.Status != InvoiceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot update invoice in {invoice.Status} status");
        }

        if (request.DueDate.HasValue) invoice.DueDate = request.DueDate.Value;
        if (request.Notes != null) invoice.Notes = request.Notes;

        invoice.UpdatedAt = DateTime.UtcNow;

        await _context.Invoices.ReplaceOneAsync(i => i.Id == id, invoice);

        _logger.LogInformation("Updated invoice {InvoiceNumber}", invoice.InvoiceNumber);

        return MapToResponse(invoice);
    }

    public async Task<InvoiceResponse?> RecordPaymentAsync(string id, RecordPaymentRequest request)
    {
        var invoice = await _context.Invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
        if (invoice == null) return null;

        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("Invoice is already fully paid");
        }

        if (request.Amount <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero");
        }

        if (request.Amount > invoice.AmountDue)
        {
            throw new ArgumentException($"Payment amount ({request.Amount}) exceeds amount due ({invoice.AmountDue})");
        }

        invoice.AmountPaid += request.Amount;
        invoice.AmountDue -= request.Amount;
        invoice.UpdatedAt = DateTime.UtcNow;

        if (invoice.AmountDue == 0)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = request.PaymentDate;
        }
        else
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        await _context.Invoices.ReplaceOneAsync(i => i.Id == id, invoice);

        _logger.LogInformation("Recorded payment of {Amount} for invoice {InvoiceNumber}", 
            request.Amount, invoice.InvoiceNumber);

        // Publish payment event if fully paid
        if (invoice.Status == InvoiceStatus.Paid)
        {
            var paymentEvent = new InvoicePaidEvent
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                AmountPaid = invoice.AmountPaid,
                PaidAt = invoice.PaidAt ?? DateTime.UtcNow
            };

            await _kafkaProducer.PublishAsync(InvoiceTopic, invoice.Id, paymentEvent);
        }

        return MapToResponse(invoice);
    }

    public async Task<bool> DeleteInvoiceAsync(string id)
    {
        var invoice = await _context.Invoices.Find(i => i.Id == id).FirstOrDefaultAsync();
        if (invoice == null) return false;

        // Can only delete draft invoices
        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot delete invoice in {invoice.Status} status");
        }

        var result = await _context.Invoices.DeleteOneAsync(i => i.Id == id);

        _logger.LogInformation("Deleted invoice {InvoiceNumber}", invoice.InvoiceNumber);

        return result.DeletedCount > 0;
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var count = await _context.Invoices.CountDocumentsAsync(_ => true);
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D6}";
    }

    private static InvoiceResponse MapToResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            OrderId = invoice.OrderId,
            CustomerId = invoice.CustomerId,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Subtotal = invoice.Subtotal,
            Tax = invoice.Tax,
            Discount = invoice.Discount,
            Total = invoice.Total,
            AmountPaid = invoice.AmountPaid,
            AmountDue = invoice.AmountDue,
            Status = invoice.Status.ToString(),
            Items = invoice.Items,
            BillingAddress = invoice.BillingAddress,
            Notes = invoice.Notes,
            PaidAt = invoice.PaidAt,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt
        };
    }
}
