using HotChocolate.Types;
using InventoryManagement.Models;

namespace InventoryManagement.GraphQL.Types;

/// <summary>
/// Excludes [NotMapped] computed properties from the Product GraphQL type.
/// EF Core cannot translate these into SQL projections, so they must not
/// appear as selectable fields.
/// </summary>
public class ProductGqlConfiguration : ObjectTypeExtension<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.Ignore(p => p.AvailableQuantity);
        descriptor.Ignore(p => p.IsLowStock);
    }
}
