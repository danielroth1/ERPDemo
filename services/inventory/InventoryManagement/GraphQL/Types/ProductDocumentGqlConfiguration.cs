using HotChocolate.Types;
using InventoryManagement.Models;

namespace InventoryManagement.GraphQL.Types;

/// <summary>
/// Hides internal/sensitive fields from the ProductDocument GraphQL type.
/// StorageKey is an internal object-storage reference that clients must not access directly.
/// Product is a bidirectional navigation property that would cause a circular type reference.
/// </summary>
public class ProductDocumentGqlConfiguration : ObjectTypeExtension<ProductDocument>
{
    protected override void Configure(IObjectTypeDescriptor<ProductDocument> descriptor)
    {
        descriptor.Ignore(d => d.StorageKey);
        descriptor.Ignore(d => d.Product);
    }
}
