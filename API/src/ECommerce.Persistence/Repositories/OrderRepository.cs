using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    // Burada sipariş sorgu ve değişiklikleri için aynı istek kapsamındaki DbContext'i hazırlıyorum.
    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni sipariş aggregate'ını kalemleriyle birlikte veritabanı takibine ekliyorum.
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    // Burada mevcut sipariş aggregate'ına eklenen yeni ödeme denemesini açıkça Added durumunda takip ediyorum.
    public async Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
    }

    // Burada siparişi yalnız gerçek sahibi için kalemleriyle birlikte takip etmeden getiriyorum.
    public Task<Order?> GetByIdForUserAsync(Guid orderId, long userId, CancellationToken cancellationToken = default)
    {
        return CreateReadGraphQuery()
            .FirstOrDefaultAsync(order => order.Id == orderId && order.UserId == userId, cancellationToken);
    }

    // Burada siparişi yönetim detay ekranı için takip etmeden ilişkileriyle getiriyorum.
    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return CreateReadGraphQuery()
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    // Burada benzersiz sipariş numarasını idempotent import anahtarı olarak kullanarak mevcut kaydı getiriyorum.
    public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return CreateReadGraphQuery()
            .FirstOrDefaultAsync(order => order.OrderNumber == orderNumber, cancellationToken);
    }

    // Burada iletişim formundaki sipariş numarasını yalnız authenticated sahibinin siparişi olarak doğruluyorum.
    public Task<Order?> GetByOrderNumberForUserAsync(string orderNumber, long userId, CancellationToken cancellationToken = default)
    {
        return _context.Orders.AsNoTracking()
            .FirstOrDefaultAsync(order => order.OrderNumber == orderNumber && order.UserId == userId, cancellationToken);
    }

    // Burada kullanıcının kendi siparişini ödeme veya iptal akışında takipli ilişkileriyle getiriyorum.
    public Task<Order?> GetByIdForUserForUpdateAsync(
        Guid orderId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return CreateUpdateGraphQuery()
            .FirstOrDefaultAsync(order => order.Id == orderId && order.UserId == userId, cancellationToken);
    }

    // Burada siparişi yönetim durum değişikliği için takipli ilişkileriyle getiriyorum.
    public Task<Order?> GetByIdForUpdateAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return CreateUpdateGraphQuery()
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    // Burada dış dünyaya açılmayan provider tokenıyla ödeme ve sipariş grafiğini tek sorgu akışında buluyorum.
    public Task<Order?> GetByPaymentProviderTokenAsync(
        string providerToken,
        bool forUpdate,
        CancellationToken cancellationToken = default)
    {
        var query = forUpdate ? CreateUpdateGraphQuery() : CreateReadGraphQuery();
        return query.FirstOrDefaultAsync(
            order => order.Payments.Any(payment => payment.ProviderToken == providerToken),
            cancellationToken);
    }

    // Burada kesin iptal edilebilir rezervasyonları belirsiz sağlayıcı denemelerinin önüne alıp kararlı sırayla takip etmeden getiriyorum.
    public async Task<IReadOnlyList<Order>> GetExpiredStockReservationsAsync(
        DateTime utcNow,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        return await CreateReadGraphQuery()
            .Where(order =>
                order.ReservationExpiresAt.HasValue &&
                order.ReservationExpiresAt.Value <= utcNow &&
                (order.Status == ECommerce.Domain.Enums.OrderStatus.Pending ||
                 order.Status == ECommerce.Domain.Enums.OrderStatus.Confirmed))
            .OrderBy(order => order.Payments.Any(payment => payment.Status == ECommerce.Domain.Enums.PaymentStatus.Pending))
            .ThenBy(order => order.ReservationExpiresAt)
            .ThenBy(order => order.Id)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    // Burada kullanıcının kendi siparişlerini güvenli owner filtresiyle sayfalıyorum.
    public Task<PagedResult<Order>> GetListForUserAsync(
        OrderListFilter filter,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return GetListInternalAsync(filter with { UserId = userId }, cancellationToken);
    }

    // Burada yönetim siparişlerini istenen filtrelerle sayfalıyorum.
    public Task<PagedResult<Order>> GetListAsync(OrderListFilter filter, CancellationToken cancellationToken = default)
    {
        return GetListInternalAsync(filter, cancellationToken);
    }

    // Burada sipariş listesini filtre, kararlı sıralama ve sınırlı sayfa boyutuyla çalıştırıyorum.
    private async Task<PagedResult<Order>> GetListInternalAsync(
        OrderListFilter filter,
        CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _context.Orders.AsNoTracking();
        if (filter.UserId.HasValue)
        {
            query = query.Where(order => order.UserId == filter.UserId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(order => order.Status == filter.Status.Value);
        }

        if (filter.CreatedFromUtc.HasValue)
        {
            query = query.Where(order => order.CreatedAt >= filter.CreatedFromUtc.Value);
        }

        if (filter.CreatedToUtc.HasValue)
        {
            query = query.Where(order => order.CreatedAt <= filter.CreatedToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = checked((filter.PageNumber - 1) * filter.PageSize);
        var items = await query
            .Include(order => order.Items)
            .AsSplitQuery()
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip(skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Order>(items, filter.PageNumber, filter.PageSize, totalCount);
    }

    // Burada yalnız okuma sipariş ekranı için kalem, ödeme ve adres snapshot grafiğini hazırlıyorum.
    private IQueryable<Order> CreateReadGraphQuery()
    {
        return CreateUpdateGraphQuery().AsNoTracking();
    }

    // Burada durum geçişi ve ödeme işlemleri için gerekli sipariş aggregate grafiğini hazırlıyorum.
    private IQueryable<Order> CreateUpdateGraphQuery()
    {
        return _context.Orders
            .Include(order => order.Items)
            .Include(order => order.Payments)
            .Include(order => order.AddressSnapshots)
            .Include(order => order.CustomerSnapshot)
            .AsSplitQuery();
    }
}
