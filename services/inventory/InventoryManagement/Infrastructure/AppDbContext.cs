using Microsoft.EntityFrameworkCore;
using InventoryManagement.Models;

namespace InventoryManagement.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<StockMovement> StockMovements { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure enum conversions
        modelBuilder.Entity<StockMovement>()
            .Property(e => e.MovementType)
            .HasConversion<string>();

        // Configure database-generated defaults
        modelBuilder.Entity<Product>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Product>()
            .Property(e => e.MinStockLevel)
            .HasDefaultValue(10);

        modelBuilder.Entity<Product>()
            .Property(e => e.MaxStockLevel)
            .HasDefaultValue(1000);

        modelBuilder.Entity<Product>()
            .Property(e => e.Unit)
            .HasDefaultValue("pcs");

        modelBuilder.Entity<Product>()
            .Property(e => e.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Product>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Product>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Category>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Category>()
            .Property(e => e.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Category>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<StockMovement>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<StockMovement>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
