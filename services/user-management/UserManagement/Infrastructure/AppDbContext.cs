using Microsoft.EntityFrameworkCore;
using UserManagement.Models;

namespace UserManagement.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Roles list conversion (can't be done with annotations)
        modelBuilder.Entity<User>()
            .Property(e => e.Roles)
            .HasConversion(
                v => string.Join(',', v.Select(r => r.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                      .Select(r => Enum.Parse<Role>(r))
                      .ToList()
            );

        // Configure database-generated defaults
        modelBuilder.Entity<User>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<User>()
            .Property(e => e.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<User>()
            .Property(e => e.EmailConfirmed)
            .HasDefaultValue(false);

        modelBuilder.Entity<User>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<User>()
            .Property(e => e.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<RefreshToken>()
            .Property(e => e.Id)
            .HasDefaultValueSql("gen_random_uuid()::text");

        modelBuilder.Entity<RefreshToken>()
            .Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
