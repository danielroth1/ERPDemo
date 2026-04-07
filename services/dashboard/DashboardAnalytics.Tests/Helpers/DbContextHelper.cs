using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DashboardAnalytics.Infrastructure;
using DashboardAnalytics.Models;

namespace DashboardAnalytics.Tests.Helpers;

public static class DbContextHelper
{
    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new TestAppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure JSONB value objects as owned types for InMemory provider
            modelBuilder.Entity<ChartData>().OwnsMany(c => c.DataPoints);

            // Configure Dictionary<string, object> jsonb properties with value converters
            var dictConverter = new ValueConverter<Dictionary<string, object>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());

            var dictComparer = new ValueComparer<Dictionary<string, object>>(
                (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                v => v == null ? 0 : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new());

            modelBuilder.Entity<Alert>()
                .Property(a => a.Data)
                .HasConversion(dictConverter)
                .Metadata.SetValueComparer(dictComparer);

            modelBuilder.Entity<DashboardMetrics>()
                .Property(m => m.Metadata)
                .HasConversion(dictConverter)
                .Metadata.SetValueComparer(dictComparer);

            modelBuilder.Entity<DatabaseAlert>()
                .Property(a => a.Metadata)
                .HasConversion(dictConverter)
                .Metadata.SetValueComparer(dictComparer);
        }
    }
}
