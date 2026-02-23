using Microsoft.EntityFrameworkCore;
using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;

namespace DashboardAnalytics.Services;

public interface IAlertService
{
    Task<Alert?> GetAlertByIdAsync(string id);
    Task<List<Alert>> GetAllAlertsAsync(int page = 1, int pageSize = 50);
    Task<List<Alert>> GetUnreadAlertsAsync();
    Task<List<Alert>> GetReadAlertsAsync();
    Task<List<Alert>> GetReadAlertsAsync(int page = 1, int pageSize = 50);
    Task<bool> MarkAsReadAsync(string id);
    Task<bool> DeleteAlertAsync(string id);
}

public class AlertService : IAlertService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AlertService> _logger;

    public AlertService(AppDbContext dbContext, ILogger<AlertService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Alert?> GetAlertByIdAsync(string id)
    {
        return await _dbContext.Alerts.FindAsync(id);
    }

    public async Task<List<Alert>> GetAllAlertsAsync(int page = 1, int pageSize = 50)
    {
        return await _dbContext.Alerts
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<Alert>> GetUnreadAlertsAsync()
    {
        return await _dbContext.Alerts
            .Where(a => !a.IsRead)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Alert>> GetReadAlertsAsync()
    {
        return await _dbContext.Alerts
            .Where(a => a.IsRead)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Alert>> GetReadAlertsAsync(int page, int pageSize)
    {
        return await _dbContext.Alerts
            .Where(a => a.IsRead)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(string id)
    {
        var alert = await _dbContext.Alerts.FindAsync(id);
        if (alert == null) return false;

        alert.IsRead = true;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAlertAsync(string id)
    {
        var alert = await _dbContext.Alerts.FindAsync(id);
        if (alert == null) return false;

        _dbContext.Alerts.Remove(alert);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
