using Microsoft.EntityFrameworkCore;
using UserManagement.Infrastructure;
using UserManagement.Models;

namespace UserManagement.Tests.Helpers;

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

            // InMemory provider doesn't support HasDefaultValueSql - use ValueGeneratedNever
            // and rely on the service to set IDs (or use a value generator)
            modelBuilder.Entity<User>()
                .Property(e => e.Id)
                .HasValueGenerator<GuidStringValueGenerator>();

            modelBuilder.Entity<RefreshToken>()
                .Property(e => e.Id)
                .HasValueGenerator<GuidStringValueGenerator>();
        }
    }

    private class GuidStringValueGenerator : Microsoft.EntityFrameworkCore.ValueGeneration.ValueGenerator<string>
    {
        public override bool GeneratesTemporaryValues => false;
        public override string Next(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
            => Guid.NewGuid().ToString();
    }
}
