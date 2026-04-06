using GreenDonut;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;

namespace InventoryManagement.GraphQL.DataLoaders;

/// <summary>
/// Batch DataLoader that loads all ProductDocument entries for a set of product IDs
/// in a single database query, solving the N+1 problem.
/// </summary>
public class ProductDocumentsByProductIdDataLoader
    : BatchDataLoader<string, IReadOnlyList<ProductDocument>>
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ProductDocumentsByProductIdDataLoader(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<string, IReadOnlyList<ProductDocument>>> LoadBatchAsync(
        IReadOnlyList<string> keys,
        CancellationToken cancellationToken)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var docs = await db.ProductDocuments
            .Where(d => keys.Contains(d.ProductId))
            .ToListAsync(cancellationToken);

        // Ensure every requested key is present (products with no documents → empty list)
        return keys.ToDictionary(
            key => key,
            key => (IReadOnlyList<ProductDocument>)docs
                .Where(d => d.ProductId == key)
                .ToList());
    }
}
