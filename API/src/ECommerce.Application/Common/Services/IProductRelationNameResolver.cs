namespace ECommerce.Application.Common.Services;

public interface IProductTypeNameResolver
{
    Task<Guid?> ResolveAsync(string? typeName, CancellationToken cancellationToken = default);
}

public interface IProductCollectionNameResolver
{
    Task<IReadOnlyList<Guid>> ResolveAsync(IEnumerable<string>? collectionNames, CancellationToken cancellationToken = default);
}
