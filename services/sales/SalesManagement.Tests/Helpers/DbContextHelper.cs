using Microsoft.EntityFrameworkCore;
using SalesManagement.Infrastructure;
using SalesManagement.Models;

namespace SalesManagement.Tests.Helpers;

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

    /// <summary>
    /// Test-specific subclass that configures Address as an owned type
    /// (InMemory provider requires explicit configuration for keyless types).
    /// </summary>
    private class TestAppDbContext : AppDbContext
    {
        public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure JSONB value objects as owned types for InMemory provider
            modelBuilder.Entity<Customer>().OwnsOne(c => c.DefaultBillingAddress);
            modelBuilder.Entity<Customer>().OwnsOne(c => c.DefaultShippingAddress);
            modelBuilder.Entity<Order>().OwnsMany(o => o.Items);
            modelBuilder.Entity<Order>().OwnsOne(o => o.ShippingAddress);
            modelBuilder.Entity<Order>().OwnsOne(o => o.BillingAddress);
            modelBuilder.Entity<Invoice>().OwnsMany(i => i.Items);
            modelBuilder.Entity<Invoice>().OwnsOne(i => i.BillingAddress);
        }
    }
}
