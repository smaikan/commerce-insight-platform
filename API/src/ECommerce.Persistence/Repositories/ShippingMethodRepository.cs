using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class ShippingMethodRepository : IShippingMethodRepository
{
    private readonly AppDbContext _context;

    // Burada kargo yöntemi sorgu ve değişiklikleri için aynı istek kapsamındaki DbContext'i hazırlıyorum.
    public ShippingMethodRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni kargo yöntemini veritabanı takibine ekliyorum.
    public async Task AddAsync(ShippingMethod shippingMethod, CancellationToken cancellationToken = default)
    {
        await _context.Set<ShippingMethod>().AddAsync(shippingMethod, cancellationToken);
    }

    // Burada kargo yöntemini kimliğiyle okuma amacıyla takip etmeden getiriyorum.
    public Task<ShippingMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<ShippingMethod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(shippingMethod => shippingMethod.Id == id, cancellationToken);
    }

    // Burada kargo yöntemini güncelleme için takipli şekilde getiriyorum.
    public Task<ShippingMethod?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<ShippingMethod>()
            .FirstOrDefaultAsync(shippingMethod => shippingMethod.Id == id, cancellationToken);
    }

    // Burada kargo yöntemlerini gösterim sırasına göre kararlı biçimde sayfalıyorum.
    public async Task<PagedResult<ShippingMethod>> GetListAsync(
        int pageNumber,
        int pageSize,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ShippingMethod> query = _context.Set<ShippingMethod>().AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(shippingMethod => shippingMethod.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = checked((pageNumber - 1) * pageSize);
        var items = await query
            .OrderBy(shippingMethod => shippingMethod.DisplayOrder)
            .ThenBy(shippingMethod => shippingMethod.Name)
            .ThenBy(shippingMethod => shippingMethod.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ShippingMethod>(items, pageNumber, pageSize, totalCount);
    }

    // Burada temizlenmiş kargo yöntemi adının başka bir kayıtta kullanılıp kullanılmadığını denetliyorum.
    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludedShippingMethodId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return _context.Set<ShippingMethod>().AnyAsync(
            shippingMethod => shippingMethod.Name == normalizedName &&
                              (!excludedShippingMethodId.HasValue || shippingMethod.Id != excludedShippingMethodId.Value),
            cancellationToken);
    }

    // Burada kargo yönteminin checkout seçiminde kullanılabilecek aktif kayda karşılık geldiğini denetliyorum.
    public Task<bool> ExistsAndActiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<ShippingMethod>()
            .AnyAsync(shippingMethod => shippingMethod.Id == id && shippingMethod.IsActive, cancellationToken);
    }
}
