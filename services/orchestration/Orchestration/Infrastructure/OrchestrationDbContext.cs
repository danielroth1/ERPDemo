using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestration.Sagas;

namespace Orchestration.Infrastructure;

public class OrchestrationDbContext : SagaDbContext
{
    public OrchestrationDbContext(DbContextOptions<OrchestrationDbContext> options) : base(options)
    {
    }

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get
        {
            yield return new PurchaseStateMap();
            yield return new ReturnStateMap();
        }
    }
}

public class PurchaseStateMap : SagaClassMap<PurchaseState>
{
    protected override void Configure(EntityTypeBuilder<PurchaseState> entity, ModelBuilder model)
    {
        entity.ToTable("purchase_saga");
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.UserId).HasMaxLength(256);
        entity.Property(x => x.ProductId).HasMaxLength(256);
        entity.Property(x => x.AuthToken).HasMaxLength(2048);
        entity.Property(x => x.ProductName).HasMaxLength(256);
        entity.Property(x => x.TransactionId).HasMaxLength(256);
        entity.Property(x => x.FailureReason).HasMaxLength(1024);
    }
}

public class ReturnStateMap : SagaClassMap<ReturnState>
{
    protected override void Configure(EntityTypeBuilder<ReturnState> entity, ModelBuilder model)
    {
        entity.ToTable("return_saga");
        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.UserId).HasMaxLength(256);
        entity.Property(x => x.ProductId).HasMaxLength(256);
        entity.Property(x => x.AuthToken).HasMaxLength(2048);
        entity.Property(x => x.ProductName).HasMaxLength(256);
        entity.Property(x => x.TransactionId).HasMaxLength(256);
        entity.Property(x => x.FailureReason).HasMaxLength(1024);
    }
}
