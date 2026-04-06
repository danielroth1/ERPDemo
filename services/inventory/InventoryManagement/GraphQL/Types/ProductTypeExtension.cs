using HotChocolate;
using InventoryManagement.GraphQL.DataLoaders;
using InventoryManagement.Models;

namespace InventoryManagement.GraphQL.Types;

/// <summary>
/// Extends the Product GraphQL type with a batch-loaded documents resolver.
/// HC strips the "Get" prefix → the field is exposed as "documents".
/// </summary>
[ExtendObjectType<Product>]
public class ProductTypeExtension
{
    public Task<IReadOnlyList<ProductDocument>> GetDocuments(
        [Parent] Product product,
        ProductDocumentsByProductIdDataLoader loader,
        CancellationToken ct)
        => loader.LoadAsync(product.Id, ct);
}
