using MediatR;

namespace ECommerce.Application.Products.Relations.Commands.UpdateProductRelations;

public sealed record UpdateProductRelationsCommand(
    long ProductId,
    IReadOnlyList<Guid> CollectionIds,
    IReadOnlyList<Guid> TagIds,
    IReadOnlyList<ProductBundleItemInput> BundleItems) : IRequest;

public sealed record ProductBundleItemInput(long ProductId, int Quantity);
