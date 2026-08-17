using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly AppDbContext _context;

    // Burada sepet sorguları ve değişiklikleri için aynı istek kapsamındaki DbContext'i hazırlıyorum.
    public CartRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni sepet aggregate'ını ilişkileriyle birlikte veritabanı takibine ekliyorum.
    public async Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        await _context.Carts.AddAsync(cart, cancellationToken);
    }

    // Burada birleştirme sonrası eski sepeti ve cascade itemlarını silmeye hazırlıyorum.
    public void Remove(Cart cart)
    {
        _context.Carts.Remove(cart);
    }

    // Burada sepeti ekran için gereken ürün ve varyant bilgileriyle takip etmeden getiriyorum.
    public Task<Cart?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return CreateReadQuery()
            .FirstOrDefaultAsync(cart => cart.Id == id, cancellationToken);
    }

    // Burada sepeti kullanıcı veya misafir owner'ı üzerinden takip etmeden getiriyorum.
    public Task<Cart?> GetByOwnerAsync(
        CartOwner owner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return ApplyOwnerFilter(CreateReadQuery(), owner)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Burada sepeti kullanıcı veya misafir owner'ı üzerinden değişiklik takibiyle getiriyorum.
    public Task<Cart?> GetByOwnerForUpdateAsync(
        CartOwner owner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return ApplyOwnerFilter(CreateGraphQuery(), owner)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Burada sepet ekranının ihtiyaç duyduğu aggregate grafiğini takip etmeden hazırlıyorum.
    private IQueryable<Cart> CreateReadQuery()
    {
        return CreateGraphQuery().AsNoTracking();
    }

    // Burada sepet itemlarıyla ürün ve varyant bilgilerini tek aggregate sorgusuna dahil ediyorum.
    private IQueryable<Cart> CreateGraphQuery()
    {
        return _context.Carts
            .Include(cart => cart.Items)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Images)
            .Include(cart => cart.Items)
                .ThenInclude(item => item.ProductVariant)
            .AsSplitQuery();
    }

    // Burada sorguyu yalnız doğrulanmış kullanıcı veya session sahibinin sepetine sınırlandırıyorum.
    private static IQueryable<Cart> ApplyOwnerFilter(
        IQueryable<Cart> query,
        CartOwner owner)
    {
        return owner.UserId.HasValue
            ? query.Where(cart => cart.UserId == owner.UserId.Value)
            : query.Where(cart => cart.UserId == null && cart.SessionId == owner.SessionId);
    }
}
