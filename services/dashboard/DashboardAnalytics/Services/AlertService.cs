using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;
using MongoDB.Driver;

namespace DashboardAnalytics.Services;

public interface IAlertService
{
    Task<Alert?> GetAlertByIdAsync(string id);
    Task<List<Alert>> GetAllAlertsAsync(int page = 1, int pageSize = 50);
    Task<List<Alert>> GetUnreadAlertsAsync();
    Task<bool> MarkAsReadAsync(string id);
    Task<bool> DeleteAlertAsync(string id);
}

public class AlertService : IAlertService
{
    private readonly MongoDbContext _context;
    private readonly ILogger<AlertService> _logger;

    public AlertService(MongoDbContext context, ILogger<AlertService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Alert?> GetAlertByIdAsync(string id)
    {
        return await _context.Alerts.Find(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Alert>> GetAllAlertsAsync(int page = 1, int pageSize = 50)
    {
        return await _context.Alerts
            .Find(_ => true)
            .SortByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<List<Alert>> GetUnreadAlertsAsync()
    {
        return await _context.Alerts
            .Find(a => !a.IsRead)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(string id)
    {
        var filter = Builders<Alert>.Filter.Eq(a => a.Id, id);
        var update = Builders<Alert>.Update.Set(a => a.IsRead, true);
        var result = await _context.Alerts.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAlertAsync(string id)
    {
        var result = await _context.Alerts.DeleteOneAsync(a => a.Id == id);
        return result.DeletedCount > 0;
    }
}
