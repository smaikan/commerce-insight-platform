using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ProductTypeRepository : IProductTypeRepository
{
    private readonly AppDbContext _context;

    public ProductTypeRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni ürün tipini veritabanı takibine ekliyorum.
    public async Task AddAsync(ProductType productType, CancellationToken cancellationToken = default)
    {
        await _context.ProductTypes.AddAsync(productType, cancellationToken);
    }

    // Burada birden fazla ürün tipini tek seferde veritabanı takibine ekliyorum.
    public async Task AddRangeAsync(IReadOnlyCollection<ProductType> productTypes, CancellationToken cancellationToken = default)
    {
        await _context.ProductTypes.AddRangeAsync(productTypes, cancellationToken);
    }

    // Burada ürün tipi kaydının var olup olmadığını kontrol ediyorum.
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductTypes.AnyAsync(type => type.Id == id, cancellationToken);
    }

    // Burada ürün tipini okuma amaçlı takip etmeden getiriyorum.
    public Task<ProductType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.Id == id, cancellationToken);
    }

    // Burada ürün tipini güncelleme için takipli şekilde getiriyorum.
    public Task<ProductType?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ProductTypes
            .FirstOrDefaultAsync(type => type.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductType>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var normalizedNames = names.Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim().ToUpperInvariant()).Distinct().ToList();
        return await _context.ProductTypes.AsNoTracking()
            .Where(type => normalizedNames.Contains(type.Name.ToUpper()))
            .ToListAsync(cancellationToken);
    }

    // Burada ürün tiplerini ada göre sıralı şekilde getiriyorum.
    public async Task<PagedResult<ProductType>> GetListAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProductTypes.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(type => type.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductType>(items, pageNumber, pageSize, totalCount);
    }

    // Burada verilen id listesindeki mevcut ürün tipi idlerini buluyorum.
    public async Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var productTypeIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var existingIds = await _context.ProductTypes
            .AsNoTracking()
            .Where(type => productTypeIds.Contains(type.Id))
            .Select(type => type.Id)
            .ToListAsync(cancellationToken);

        return existingIds.ToHashSet();
    }

    // Burada verilen isimlerden veritabanında olan ürün tipi isimlerini buluyorum.
    public async Task<IReadOnlySet<string>> GetExistingNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var normalizedNames = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingNames = await _context.ProductTypes
            .AsNoTracking()
            .Where(type => normalizedNames.Contains(type.Name))
            .Select(type => type.Name)
            .ToListAsync(cancellationToken);

        return existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // Burada ürün tipi adının başka bir kayıtta kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> NameExistsAsync(string name, Guid? excludedProductTypeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();

        return _context.ProductTypes.AnyAsync(
            type => type.Name == normalizedName && (!excludedProductTypeId.HasValue || type.Id != excludedProductTypeId.Value),
            cancellationToken);
    }
}
