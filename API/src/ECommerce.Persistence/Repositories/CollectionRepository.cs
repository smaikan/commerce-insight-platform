using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
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

    public async Task<IReadOnlyList<Collection>> GetByNamesOrUrlsAsync(
        IEnumerable<string> names, IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var normalizedNames = names.Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim().ToUpperInvariant()).Distinct().ToList();
        var normalizedUrls = urls.Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim().ToUpperInvariant()).Distinct().ToList();
        return await _context.Collections.AsNoTracking()
            .Where(collection => normalizedNames.Contains(collection.Name.ToUpper()) || normalizedUrls.Contains(collection.Url.ToUpper()))
            .ToListAsync(cancellationToken);
    }

    // Burada koleksiyonları gösterim sırasına göre getiriyorum.
    public async Task<PagedResult<Collection>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Collections.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(collection => collection.DisplayOrder)
            .ThenBy(collection => collection.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Collection>(items, pageNumber, pageSize, totalCount);
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
