using Microsoft.EntityFrameworkCore;
using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;

namespace DashboardAnalytics.Services;

public interface IKPIService
{
    Task<KPI> CreateKPIAsync(CreateKPIRequest request);
    Task<KPI?> GetKPIByIdAsync(string id);
    Task<List<KPI>> GetAllKPIsAsync();
    Task<KPI?> UpdateKPIAsync(string id, UpdateKPIRequest request);
    Task<bool> DeleteKPIAsync(string id);
}

public class KPIService : IKPIService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<KPIService> _logger;

    public KPIService(AppDbContext dbContext, ILogger<KPIService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<KPI> CreateKPIAsync(CreateKPIRequest request)
    {
        var kpi = new KPI
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            TargetValue = request.TargetValue,
            CurrentValue = 0,
            PreviousValue = 0,
            Status = KPIStatus.OnTrack,
            LastUpdated = DateTime.UtcNow
        };

        _dbContext.KPIs.Add(kpi);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Created KPI: {Name}", kpi.Name);
        return kpi;
    }

    public async Task<KPI?> GetKPIByIdAsync(string id)
    {
        return await _dbContext.KPIs.FindAsync(id);
    }

    public async Task<List<KPI>> GetAllKPIsAsync()
    {
        return await _dbContext.KPIs.ToListAsync();
    }

    public async Task<KPI?> UpdateKPIAsync(string id, UpdateKPIRequest request)
    {
        var kpi = await GetKPIByIdAsync(id);
        if (kpi == null) return null;

        kpi.PreviousValue = kpi.CurrentValue;
        kpi.CurrentValue = request.CurrentValue;
        
        if (request.TargetValue.HasValue)
            kpi.TargetValue = request.TargetValue.Value;

        // Calculate status based on progress
        var progress = kpi.TargetValue > 0 ? (kpi.CurrentValue / kpi.TargetValue) * 100 : 0;
        kpi.Status = progress switch
        {
            >= 90 => KPIStatus.OnTrack,
            >= 70 => KPIStatus.NeedsAttention,
            _ => KPIStatus.Critical
        };

        kpi.LastUpdated = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation("Updated KPI: {Name}", kpi.Name);
        return kpi;
    }

    public async Task<bool> DeleteKPIAsync(string id)
    {
        var kpi = await _dbContext.KPIs.FindAsync(id);
        if (kpi == null) return false;

        _dbContext.KPIs.Remove(kpi);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}
