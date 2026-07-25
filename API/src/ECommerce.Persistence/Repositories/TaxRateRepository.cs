using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class TaxRateRepository : ITaxRateRepository
{
    private readonly AppDbContext _context;

    // Burada vergi oranı sorgu ve değişiklikleri için aynı istek kapsamındaki DbContext'i hazırlıyorum.
    public TaxRateRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni vergi oranını veritabanı takibine ekliyorum.
    public async Task AddAsync(TaxRate taxRate, CancellationToken cancellationToken = default)
    {
        await _context.Set<TaxRate>().AddAsync(taxRate, cancellationToken);
    }

    // Burada vergi oranını kimliğiyle okuma amacıyla takip etmeden getiriyorum.
    public Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<TaxRate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(taxRate => taxRate.Id == id, cancellationToken);
    }

    // Burada vergi oranını güncelleme için takipli şekilde getiriyorum.
    public Task<TaxRate?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<TaxRate>()
            .FirstOrDefaultAsync(taxRate => taxRate.Id == id, cancellationToken);
    }

    // Burada vergi oranlarını ada göre kararlı sıralayıp istenen sayfayı getiriyorum.
    public async Task<PagedResult<TaxRate>> GetListAsync(
        int pageNumber,
        int pageSize,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TaxRate> query = _context.Set<TaxRate>().AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(taxRate => taxRate.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = checked((pageNumber - 1) * pageSize);
        var items = await query
            .OrderBy(taxRate => taxRate.Name)
            .ThenBy(taxRate => taxRate.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaxRate>(items, pageNumber, pageSize, totalCount);
    }

    // Burada temizlenmiş vergi oranı adının başka bir kayıtta kullanılıp kullanılmadığını denetliyorum.
    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludedTaxRateId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return _context.Set<TaxRate>().AnyAsync(
            taxRate => taxRate.Name == normalizedName &&
                       (!excludedTaxRateId.HasValue || taxRate.Id != excludedTaxRateId.Value),
            cancellationToken);
    }

    // Burada vergi oranının ürün veya checkout seçiminde kullanılabilecek aktif kayda karşılık geldiğini denetliyorum.
    public Task<bool> ExistsAndActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<TaxRate>()
            .AnyAsync(taxRate => taxRate.Id == id && taxRate.IsActive, cancellationToken);
    }

    // Burada toplu ürün isteğinde verilen kimliklerden aktif olarak bulunanları tek sorguda çıkarıyorum.
    public async Task<IReadOnlySet<Guid>> GetActiveExistingIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var taxRateIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var existingIds = await _context.Set<TaxRate>()
            .AsNoTracking()
            .Where(taxRate => taxRateIds.Contains(taxRate.Id) && taxRate.IsActive)
            .Select(taxRate => taxRate.Id)
            .ToListAsync(cancellationToken);

        return existingIds.ToHashSet();
    }

    // Burada toplu ürün oluşturma fiyatları için etkin vergi oranlarını tek sorguda kimlikleriyle eşliyorum.
    public async Task<IReadOnlyDictionary<Guid, TaxRate>> GetActiveByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var taxRateIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var taxRates = await _context.Set<TaxRate>()
            .AsNoTracking()
            .Where(taxRate => taxRateIds.Contains(taxRate.Id) && taxRate.IsActive)
            .ToListAsync(cancellationToken);

        return taxRates.ToDictionary(taxRate => taxRate.Id);
    }
}
