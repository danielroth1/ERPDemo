using MongoDB.Driver;
using SalesManagement.Infrastructure;
using SalesManagement.Models;
using SalesManagement.Models.DTOs;

namespace SalesManagement.Services;

public interface ICustomerService
{
    Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> GetCustomerByIdAsync(string id);
    Task<CustomerResponse?> GetCustomerByEmailAsync(string email);
    Task<List<CustomerResponse>> GetAllCustomersAsync(int skip = 0, int limit = 100);
    Task<List<CustomerResponse>> SearchCustomersAsync(string searchTerm, int skip = 0, int limit = 100);
    Task<CustomerResponse?> UpdateCustomerAsync(string id, UpdateCustomerRequest request);
    Task<bool> DeleteCustomerAsync(string id);
}

public class CustomerService : ICustomerService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(MongoDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Check if email already exists
        var existing = await _context.Customers
            .Find(c => c.Email == request.Email)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            throw new InvalidOperationException($"Customer with email {request.Email} already exists");
        }

        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            TaxId = request.TaxId,
            DefaultBillingAddress = request.DefaultBillingAddress != null ? MapAddress(request.DefaultBillingAddress) : null,
            DefaultShippingAddress = request.DefaultShippingAddress != null ? MapAddress(request.DefaultShippingAddress) : null,
            Notes = request.Notes
        };

        await _context.Customers.InsertOneAsync(customer);

        _logger.LogInformation("Created customer {Email}", customer.Email);

        return MapToResponse(customer);
    }

    public async Task<CustomerResponse?> GetCustomerByIdAsync(string id)
    {
        var customer = await _context.Customers.Find(c => c.Id == id).FirstOrDefaultAsync();
        return customer != null ? MapToResponse(customer) : null;
    }

    public async Task<CustomerResponse?> GetCustomerByEmailAsync(string email)
    {
        var customer = await _context.Customers.Find(c => c.Email == email).FirstOrDefaultAsync();
        return customer != null ? MapToResponse(customer) : null;
    }

    public async Task<List<CustomerResponse>> GetAllCustomersAsync(int skip = 0, int limit = 100)
    {
        var customers = await _context.Customers
            .Find(c => c.IsActive)
            .SortBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return customers.Select(MapToResponse).ToList();
    }

    public async Task<List<CustomerResponse>> SearchCustomersAsync(string searchTerm, int skip = 0, int limit = 100)
    {
        var filter = Builders<Customer>.Filter.And(
            Builders<Customer>.Filter.Eq(c => c.IsActive, true),
            Builders<Customer>.Filter.Or(
                Builders<Customer>.Filter.Regex(c => c.FirstName, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                Builders<Customer>.Filter.Regex(c => c.LastName, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                Builders<Customer>.Filter.Regex(c => c.Email, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                Builders<Customer>.Filter.Regex(c => c.Company, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
            )
        );

        var customers = await _context.Customers
            .Find(filter)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync();

        return customers.Select(MapToResponse).ToList();
    }

    public async Task<CustomerResponse?> UpdateCustomerAsync(string id, UpdateCustomerRequest request)
    {
        var customer = await _context.Customers.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (customer == null) return null;

        if (request.FirstName != null) customer.FirstName = request.FirstName;
        if (request.LastName != null) customer.LastName = request.LastName;
        if (request.Email != null)
        {
            // Check if new email already exists
            var existing = await _context.Customers
                .Find(c => c.Email == request.Email && c.Id != id)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                throw new InvalidOperationException($"Customer with email {request.Email} already exists");
            }

            customer.Email = request.Email;
        }
        if (request.Phone != null) customer.Phone = request.Phone;
        if (request.Company != null) customer.Company = request.Company;
        if (request.TaxId != null) customer.TaxId = request.TaxId;
        if (request.DefaultBillingAddress != null) customer.DefaultBillingAddress = MapAddress(request.DefaultBillingAddress);
        if (request.DefaultShippingAddress != null) customer.DefaultShippingAddress = MapAddress(request.DefaultShippingAddress);
        if (request.Notes != null) customer.Notes = request.Notes;
        if (request.IsActive.HasValue) customer.IsActive = request.IsActive.Value;

        customer.UpdatedAt = DateTime.UtcNow;

        await _context.Customers.ReplaceOneAsync(c => c.Id == id, customer);

        _logger.LogInformation("Updated customer {Email}", customer.Email);

        return MapToResponse(customer);
    }

    public async Task<bool> DeleteCustomerAsync(string id)
    {
        var customer = await _context.Customers.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (customer == null) return false;

        // Soft delete
        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.Customers.ReplaceOneAsync(c => c.Id == id, customer);

        _logger.LogInformation("Deactivated customer {Email}", customer.Email);

        return true;
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

    private static CustomerResponse MapToResponse(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            Company = customer.Company,
            TaxId = customer.TaxId,
            DefaultBillingAddress = customer.DefaultBillingAddress,
            DefaultShippingAddress = customer.DefaultShippingAddress,
            Notes = customer.Notes,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}
