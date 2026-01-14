using Microsoft.EntityFrameworkCore;
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
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(AppDbContext dbContext, ILogger<CustomerService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Check if email already exists
        var existing = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.Email == request.Email);

        if (existing != null)
        {
            throw new InvalidOperationException($"Customer with email {request.Email} already exists");
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Company = request.Company,
            TaxId = request.TaxId,
            DefaultBillingAddress = request.DefaultBillingAddress != null ? MapAddress(request.DefaultBillingAddress) : null,
            DefaultShippingAddress = request.DefaultShippingAddress != null ? MapAddress(request.DefaultShippingAddress) : null,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created customer {Email}", customer.Email);

        return MapToResponse(customer);
    }

    public async Task<CustomerResponse?> GetCustomerByIdAsync(string id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        return customer != null ? MapToResponse(customer) : null;
    }

    public async Task<CustomerResponse?> GetCustomerByEmailAsync(string email)
    {
        var customer = await _dbContext.Customers.FirstOrDefaultAsync(c => c.Email == email);
        return customer != null ? MapToResponse(customer) : null;
    }

    public async Task<List<CustomerResponse>> GetAllCustomersAsync(int skip = 0, int limit = 100)
    {
        var customers = await _dbContext.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        return customers.Select(MapToResponse).ToList();
    }

    public async Task<List<CustomerResponse>> SearchCustomersAsync(string searchTerm, int skip = 0, int limit = 100)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        var customers = await _dbContext.Customers
            .Where(c => c.IsActive &&
                (c.FirstName.ToLower().Contains(lowerSearchTerm) ||
                 c.LastName.ToLower().Contains(lowerSearchTerm) ||
                 c.Email.ToLower().Contains(lowerSearchTerm) ||
                 (c.Company != null && c.Company.ToLower().Contains(lowerSearchTerm))))
            .Skip(skip)
            .Take(limit)
            .ToListAsync();

        return customers.Select(MapToResponse).ToList();
    }

    public async Task<CustomerResponse?> UpdateCustomerAsync(string id, UpdateCustomerRequest request)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null) return null;

        if (request.FirstName != null) customer.FirstName = request.FirstName;
        if (request.LastName != null) customer.LastName = request.LastName;
        if (request.Email != null)
        {
            // Check if new email already exists
            var existing = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.Email == request.Email && c.Id != id);

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

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Updated customer {Email}", customer.Email);

        return MapToResponse(customer);
    }

    public async Task<bool> DeleteCustomerAsync(string id)
    {
        var customer = await _dbContext.Customers.FindAsync(id);
        if (customer == null) return false;

        // Soft delete
        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

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
