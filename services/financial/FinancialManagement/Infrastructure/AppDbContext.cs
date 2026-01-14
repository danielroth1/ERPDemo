using Microsoft.EntityFrameworkCore;
using FinancialManagement.Models;

namespace FinancialManagement.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Budget> Budgets { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure enum to string conversions (can't be done with annotations)
        modelBuilder.Entity<Account>()
            .Property(e => e.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Account>()
            .Property(e => e.Category)
            .HasConversion<string>();

        modelBuilder.Entity<Transaction>()
            .Property(e => e.Type)
            .HasConversion<string>();

        modelBuilder.Entity<Transaction>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Budget>()
            .Property(e => e.Period)
            .HasConversion<string>();

        // Configure database-generated default values (can't be done with annotations)
        modelBuilder.Entity<Account>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Account>()
            .Property(e => e.Balance)
            .HasDefaultValue(0);

        modelBuilder.Entity<Account>()
            .Property(e => e.Currency)
            .HasDefaultValue("USD");

        modelBuilder.Entity<Account>()
            .Property(e => e.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Account>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Account>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Transaction>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Transaction>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Transaction>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Budget>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<Budget>()
            .Property(e => e.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<Budget>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Budget>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
