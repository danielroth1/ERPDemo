using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Models;
using DashboardAnalytics.Models.DTOs;
using MongoDB.Driver;

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
    private readonly MongoDbContext _context;
    private readonly ILogger<KPIService> _logger;

    public KPIService(MongoDbContext context, ILogger<KPIService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<KPI> CreateKPIAsync(CreateKPIRequest request)
    {
        var kpi = new KPI
        {
            Name = request.Name,
            Description = request.Description,
            TargetValue = request.TargetValue,
            CurrentValue = 0,
            PreviousValue = 0,
            Status = KPIStatus.OnTrack
        };

        await _context.KPIs.InsertOneAsync(kpi);
        _logger.LogInformation("Created KPI: {Name}", kpi.Name);
        return kpi;
    }

    public async Task<KPI?> GetKPIByIdAsync(string id)
    {
        return await _context.KPIs.Find(k => k.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<KPI>> GetAllKPIsAsync()
    {
        return await _context.KPIs.Find(_ => true).ToListAsync();
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

        var filter = Builders<KPI>.Filter.Eq(k => k.Id, id);
        await _context.KPIs.ReplaceOneAsync(filter, kpi);
        
        _logger.LogInformation("Updated KPI: {Name}", kpi.Name);
        return kpi;
    }

    public async Task<bool> DeleteKPIAsync(string id)
    {
        var result = await _context.KPIs.DeleteOneAsync(k => k.Id == id);
        return result.DeletedCount > 0;
    }
}
