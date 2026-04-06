using HotChocolate;
using HotChocolate.Data;
using InventoryManagement.Infrastructure;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.GraphQL;

public class Query
{
    /// <summary>
    /// Returns all products. Supports offset paging, filtering (e.g. isActive),
    /// and field-level projection so the database only fetches requested columns.
    /// </summary>
    [UseOffsetPaging(DefaultPageSize = 20, MaxPageSize = 100, IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    public IQueryable<Product> GetProducts([Service] AppDbContext db)
        => db.Products.OrderBy(p => p.Name);

    /// <summary>
    /// Returns a single product by id. Documents are resolved via the batch DataLoader.
    /// </summary>
    public async Task<Product?> GetProduct(
        string id,
        [Service] AppDbContext db,
        CancellationToken ct)
        => await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
}
