using Microsoft.EntityFrameworkCore;
using DashboardAnalytics.Models;

namespace DashboardAnalytics.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DashboardMetrics> Metrics { get; set; } = null!;
    public DbSet<KPI> KPIs { get; set; } = null!;
    public DbSet<ChartData> Charts { get; set; } = null!;
    public DbSet<Alert> Alerts { get; set; } = null!;
    public DbSet<QueryExecution> QueryExecutions { get; set; } = null!;
    public DbSet<DatabaseAlert> DatabaseAlerts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure enum conversions
        modelBuilder.Entity<DashboardMetrics>()
            .Property(e => e.Type)
            .HasConversion<string>();

        modelBuilder.Entity<KPI>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<ChartData>()
            .Property(e => e.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Alert>()
            .Property(e => e.Severity)
            .HasConversion<string>();

        // Configure database-generated defaults
        modelBuilder.Entity<DashboardMetrics>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<DashboardMetrics>()
            .Property(e => e.Timestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<KPI>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<KPI>()
            .Property(e => e.LastUpdated)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<ChartData>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<ChartData>()
            .Property(e => e.GeneratedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Alert>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Alert>()
            .Property(e => e.IsRead)
            .HasDefaultValue(false);

        modelBuilder.Entity<Alert>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<QueryExecution>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<QueryExecution>()
            .Property(e => e.ExecutedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<DatabaseAlert>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<DatabaseAlert>()
            .Property(e => e.IsResolved)
            .HasDefaultValue(false);

        modelBuilder.Entity<DatabaseAlert>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
