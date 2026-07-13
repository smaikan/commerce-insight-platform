using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class CollectionRepository : ICollectionRepository
{
    private readonly AppDbContext _context;

    public CollectionRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni koleksiyonu veritabanı takibine ekliyorum.
    public async Task AddAsync(Collection collection, CancellationToken cancellationToken = default)
    {
        await _context.Collections.AddAsync(collection, cancellationToken);
    }

    // Burada birden fazla koleksiyonu tek seferde veritabanı takibine ekliyorum.
    public async Task AddRangeAsync(IReadOnlyCollection<Collection> collections, CancellationToken cancellationToken = default)
    {
        await _context.Collections.AddRangeAsync(collections, cancellationToken);
    }

    // Burada koleksiyonu okuma amaçlı takip etmeden getiriyorum.
    public Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Collections
            .AsNoTracking()
            .FirstOrDefaultAsync(collection => collection.Id == id, cancellationToken);
    }

    // Burada koleksiyonu güncelleme için takipli şekilde getiriyorum.
    public Task<Collection?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Collections
            .FirstOrDefaultAsync(collection => collection.Id == id, cancellationToken);
    }

    // Burada koleksiyonları gösterim sırasına göre getiriyorum.
    public async Task<IReadOnlyList<Collection>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Collections
            .AsNoTracking()
            .OrderBy(collection => collection.DisplayOrder)
            .ThenBy(collection => collection.Name)
            .ToListAsync(cancellationToken);
    }

    // Burada verilen id listesindeki mevcut koleksiyon idlerini buluyorum.
    public async Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var collectionIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var existingIds = await _context.Collections
            .AsNoTracking()
            .Where(collection => collectionIds.Contains(collection.Id))
            .Select(collection => collection.Id)
            .ToListAsync(cancellationToken);

        return existingIds.ToHashSet();
    }

    // Burada listedeki URL değerlerinden veritabanında olan koleksiyon URL'lerini buluyorum.
    public async Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var normalizedUrls = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingUrls = await _context.Collections
            .AsNoTracking()
            .Where(collection => normalizedUrls.Contains(collection.Url))
            .Select(collection => collection.Url)
            .ToListAsync(cancellationToken);

        return existingUrls.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // Burada koleksiyon URL bilgisinin başka bir koleksiyonda kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> UrlExistsAsync(string url, Guid? excludedCollectionId = null, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = url.Trim();

        return _context.Collections.AnyAsync(
            collection => collection.Url == normalizedUrl && (!excludedCollectionId.HasValue || collection.Id != excludedCollectionId.Value),
            cancellationToken);
    }
}
