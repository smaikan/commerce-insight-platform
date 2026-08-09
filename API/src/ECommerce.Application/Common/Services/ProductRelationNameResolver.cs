using System.Security.Cryptography;
using System.Text;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Services;

public sealed class ProductTypeNameResolver : IProductTypeNameResolver
{
    private readonly IProductTypeRepository _types;
    private readonly Dictionary<string, Guid> _resolvedIds = new(StringComparer.OrdinalIgnoreCase);
    public ProductTypeNameResolver(IProductTypeRepository types) => _types = types;

    public async Task<Guid?> ResolveAsync(string? typeName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;
        var name = typeName.Trim();
        if (_resolvedIds.TryGetValue(name, out var resolvedId)) return resolvedId;
        var existing = await _types.GetByNamesAsync([name], cancellationToken) ?? [];
        if (existing.Count > 0)
        {
            _resolvedIds[name] = existing[0].Id;
            return existing[0].Id;
        }
        var type = new ProductType(name);
        await _types.AddAsync(type, cancellationToken);
        _resolvedIds[name] = type.Id;
        return type.Id;
    }
}

public sealed class ProductCollectionNameResolver : IProductCollectionNameResolver
{
    private const int MaximumUrlLength = 200;
    private readonly ICollectionRepository _collections;
    private readonly Func<string, string> _generateUrl;
    private readonly Dictionary<string, Guid> _resolvedIds = new(StringComparer.OrdinalIgnoreCase);
    public ProductCollectionNameResolver(ICollectionRepository collections, IUrlGenerator urlGenerator)
    {
        _collections = collections;
        _generateUrl = urlGenerator.Generate;
    }

    public async Task<IReadOnlyList<Guid>> ResolveAsync(IEnumerable<string>? collectionNames, CancellationToken cancellationToken = default)
    {
        var names = (collectionNames ?? []).Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (names.Count == 0) return [];

        var uncachedNames = names.Where(name => !_resolvedIds.ContainsKey(name)).ToList();
        if (uncachedNames.Count == 0) return names.Select(name => _resolvedIds[name]).ToList();

        var baseUrls = uncachedNames.Select(_generateUrl).ToList();
        var existing = await _collections.GetByNamesOrUrlsAsync(uncachedNames, baseUrls, cancellationToken) ?? [];
        var byName = existing.ToDictionary(collection => collection.Name, StringComparer.OrdinalIgnoreCase);
        var occupiedUrls = existing.Select(collection => collection.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newCollections = new List<Collection>();

        foreach (var name in uncachedNames)
        {
            if (byName.ContainsKey(name)) continue;
            var baseUrl = _generateUrl(name);
            var url = occupiedUrls.Contains(baseUrl) ? AppendSuffix(baseUrl, CreateHash(name)) : baseUrl;
            while (occupiedUrls.Contains(url)) url = AppendSuffix(baseUrl, Guid.NewGuid().ToString("N")[..12]);
            var collection = new Collection(name, url);
            newCollections.Add(collection);
            byName[name] = collection;
            occupiedUrls.Add(url);
        }

        if (newCollections.Count > 0) await _collections.AddRangeAsync(newCollections, cancellationToken);
        foreach (var collection in byName) _resolvedIds[collection.Key] = collection.Value.Id;
        return names.Select(name => _resolvedIds[name]).ToList();
    }

    private static string CreateHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.ToUpperInvariant())))[..8].ToLowerInvariant();
    private static string AppendSuffix(string value, string suffix)
    {
        var maximumBaseLength = MaximumUrlLength - suffix.Length - 1;
        return $"{(value.Length > maximumBaseLength ? value[..maximumBaseLength].TrimEnd('-') : value)}-{suffix}";
    }
}
