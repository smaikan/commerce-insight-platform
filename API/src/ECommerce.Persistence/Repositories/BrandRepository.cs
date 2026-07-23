using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class BrandRepository : IBrandRepository
{
    private readonly AppDbContext _context;

    public BrandRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni markayı veritabanı takibine ekliyorum.
    public async Task AddAsync(Brand brand, CancellationToken cancellationToken = default)
    {
        await _context.Brands.AddAsync(brand, cancellationToken);
    }

    // Burada birden fazla markayı tek seferde veritabanı takibine ekliyorum.
    public async Task AddRangeAsync(IReadOnlyCollection<Brand> brands, CancellationToken cancellationToken = default)
    {
        await _context.Brands.AddRangeAsync(brands, cancellationToken);
    }

    // Burada marka kaydının var olup olmadığını kontrol ediyorum.
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Brands.AnyAsync(brand => brand.Id == id, cancellationToken);
    }

    // Burada markayı okuma amaçlı takip etmeden getiriyorum.
    public Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Brands
            .AsNoTracking()
            .FirstOrDefaultAsync(brand => brand.Id == id, cancellationToken);
    }

    // Burada markayı güncelleme için takipli şekilde getiriyorum.
    public Task<Brand?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Brands
            .FirstOrDefaultAsync(brand => brand.Id == id, cancellationToken);
    }

    // Burada markaları ada göre sıralı şekilde getiriyorum.
    public async Task<PagedResult<Brand>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Brands.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(brand => brand.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Brand>(items, pageNumber, pageSize, totalCount);
    }

    // Burada verilen id listesindeki mevcut marka idlerini buluyorum.
    public async Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var brandIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var existingIds = await _context.Brands
            .AsNoTracking()
            .Where(brand => brandIds.Contains(brand.Id))
            .Select(brand => brand.Id)
            .ToListAsync(cancellationToken);

        return existingIds.ToHashSet();
    }

    // Burada listedeki URL değerlerinden veritabanında olan marka URL'lerini buluyorum.
    public async Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var normalizedUrls = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingUrls = await _context.Brands
            .AsNoTracking()
            .Where(brand => normalizedUrls.Contains(brand.Url))
            .Select(brand => brand.Url)
            .ToListAsync(cancellationToken);

        return existingUrls.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // Burada marka URL bilgisinin başka bir markada kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> UrlExistsAsync(string url, Guid? excludedBrandId = null, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = url.Trim();

        return _context.Brands.AnyAsync(
            brand => brand.Url == normalizedUrl && (!excludedBrandId.HasValue || brand.Id != excludedBrandId.Value),
            cancellationToken);
    }
}
