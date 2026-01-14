using Microsoft.EntityFrameworkCore;
using SalesManagement.Models;

namespace SalesManagement.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure enum conversions
        modelBuilder.Entity<Order>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Invoice>()
            .Property(e => e.Status)
            .HasConversion<string>();

        // Configure database-generated defaults
        modelBuilder.Entity<Order>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Order>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Order>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Customer>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Customer>()
            .Property(e => e.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Customer>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Customer>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Invoice>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Invoice>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Invoice>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
