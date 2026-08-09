using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class GuestOrderRepository : IGuestOrderRepository
{
    private readonly AppDbContext _context;

    // Burada guest sipariş güvenlik ve erişim sorguları için aynı scoped DbContext'i hazırlıyorum.
    public GuestOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni guest session kaydını ekliyorum.
    public Task AddSessionAsync(GuestOrderSession session, CancellationToken cancellationToken = default) =>
        _context.GuestOrderSessions.AddAsync(session, cancellationToken).AsTask();

    // Burada yeni guest sipariş erişim yetkisini ekliyorum.
    public Task AddAccessGrantAsync(GuestOrderAccessGrant grant, CancellationToken cancellationToken = default) =>
        _context.GuestOrderAccessGrants.AddAsync(grant, cancellationToken).AsTask();

    // Burada yeni tek kullanımlık magic-link kaydını ekliyorum.
    public Task AddMagicLinkAsync(GuestOrderMagicLink link, CancellationToken cancellationToken = default) =>
        _context.GuestOrderMagicLinks.AddAsync(link, cancellationToken).AsTask();

    // Burada yeni checkout idempotency sonucunu ekliyorum.
    public Task AddIdempotencyAsync(GuestCheckoutIdempotency record, CancellationToken cancellationToken = default) =>
        _context.GuestCheckoutIdempotencies.AddAsync(record, cancellationToken).AsTask();

    // Burada hash ile aktifliği sonradan domain tarafından denetlenecek session kaydını takipli getiriyorum.
    public Task<GuestOrderSession?> GetSessionForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.GuestOrderSessions.FirstOrDefaultAsync(session => session.TokenHash == tokenHash, cancellationToken);

    // Burada hash ile tek kullanımlık magic-link kaydını takipli getiriyorum.
    public Task<GuestOrderMagicLink?> GetMagicLinkForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.GuestOrderMagicLinks.FirstOrDefaultAsync(link => link.TokenHash == tokenHash, cancellationToken);

    // Burada erişim e-postası üretmek için sipariş numarası ve e-postayı yalnız sahipsiz siparişi bulmada kullanıyorum.
    public Task<Order?> GetUnclaimedOrderForAccessLinkAsync(
        string orderNumber,
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        _context.Orders
            .AsNoTracking()
            .Include(order => order.CustomerSnapshot)
            .FirstOrDefaultAsync(order =>
            !order.UserId.HasValue && order.OrderNumber == orderNumber &&
            order.CustomerSnapshot != null && order.CustomerSnapshot.Email == normalizedEmail,
            cancellationToken);

    // Burada session ile sipariş arasında iptal edilmemiş erişim grant'i olup olmadığını denetliyorum.
    public Task<bool> HasActiveAccessGrantAsync(Guid sessionId, Guid orderId, CancellationToken cancellationToken = default) =>
        _context.GuestOrderAccessGrants.AnyAsync(
            grant => grant.SessionId == sessionId && grant.OrderId == orderId && !grant.RevokedAt.HasValue,
            cancellationToken);

    // Burada aynı session-sipariş çifti için aktif veya iptal edilmiş mevcut yetki kaydını takipli getiriyorum.
    public Task<GuestOrderAccessGrant?> GetAccessGrantForUpdateAsync(
        Guid sessionId,
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        _context.GuestOrderAccessGrants.FirstOrDefaultAsync(
            grant => grant.SessionId == sessionId && grant.OrderId == orderId,
            cancellationToken);

    // Burada yalnız aktif session grant'ine bağlı sipariş grafiğini getiriyorum.
    public Task<Order?> GetOrderForSessionAsync(
        Guid sessionId,
        Guid orderId,
        bool forUpdate,
        CancellationToken cancellationToken = default)
    {
        var query = CreateOrderGraphQuery();
        if (!forUpdate)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(order =>
            order.Id == orderId &&
            _context.GuestOrderAccessGrants.Any(grant =>
                grant.SessionId == sessionId && grant.OrderId == order.Id && !grant.RevokedAt.HasValue),
            cancellationToken);
    }

    // Burada yalnız session'ın sipariş grant'i altındaki iade talebini getiriyorum.
    public Task<ReturnRequest?> GetReturnForSessionAsync(
        Guid sessionId,
        Guid orderId,
        Guid returnId,
        CancellationToken cancellationToken = default)
    {
        return _context.ReturnRequests
            .AsNoTracking()
            .Include(request => request.Items)
            .FirstOrDefaultAsync(request =>
                request.Id == returnId && request.OrderId == orderId &&
                _context.GuestOrderAccessGrants.Any(grant =>
                    grant.SessionId == sessionId && grant.OrderId == orderId && !grant.RevokedAt.HasValue),
                cancellationToken);
    }

    // Burada session'ın erişebildiği siparişleri sınırlı ve kararlı biçimde sayfalıyorum.
    public async Task<PagedResult<Order>> GetOrdersForSessionAsync(
        Guid sessionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking().Where(order =>
            _context.GuestOrderAccessGrants.Any(grant =>
                grant.SessionId == sessionId && grant.OrderId == order.Id && !grant.RevokedAt.HasValue));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(order => order.Items)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip(checked((pageNumber - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Order>(items, pageNumber, pageSize, totalCount);
    }

    // Burada guest session'ın eriştiği siparişin iade taleplerini sayfalıyorum.
    public async Task<PagedResult<ReturnRequest>> GetReturnsForSessionOrderAsync(
        Guid sessionId,
        Guid orderId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await HasActiveAccessGrantAsync(sessionId, orderId, cancellationToken);
        if (!hasAccess)
        {
            return new PagedResult<ReturnRequest>([], pageNumber, pageSize, 0);
        }

        var query = _context.ReturnRequests.AsNoTracking().Where(request => request.OrderId == orderId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Include(request => request.Items)
            .OrderByDescending(request => request.CreatedAt)
            .ThenByDescending(request => request.Id)
            .Skip(checked((pageNumber - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<ReturnRequest>(items, pageNumber, pageSize, totalCount);
    }

    // Burada cart ve idempotency hash birleşimiyle önceki checkout sonucunu takipli getiriyorum.
    public Task<GuestCheckoutIdempotency?> GetIdempotencyForUpdateAsync(
        string cartSessionHash,
        string keyHash,
        CancellationToken cancellationToken = default) =>
        _context.GuestCheckoutIdempotencies
            .Include(record => record.Order)
                .ThenInclude(order => order.Items)
            .Include(record => record.Order)
                .ThenInclude(order => order.Payments)
            .Include(record => record.Order)
                .ThenInclude(order => order.AddressSnapshots)
            .Include(record => record.Order)
                .ThenInclude(order => order.CustomerSnapshot)
            .AsSplitQuery()
            .FirstOrDefaultAsync(record => record.CartSessionHash == cartSessionHash && record.KeyHash == keyHash, cancellationToken);

    // Burada aynı session veya e-posta için halen ödeme bekleyen aktif rezervasyonları sayıyorum.
    public Task<int> CountActiveUnpaidOrdersAsync(
        Guid sessionId,
        string emailHash,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        return _context.Orders.CountAsync(order =>
            !order.UserId.HasValue &&
            order.ReservationExpiresAt.HasValue && order.ReservationExpiresAt > utcNow &&
            (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Confirmed) &&
            (_context.GuestOrderAccessGrants.Any(grant =>
                 grant.SessionId == sessionId && grant.OrderId == order.Id && !grant.RevokedAt.HasValue) ||
             _context.GuestOrderMagicLinks.Any(link =>
                 link.OrderId == order.Id && link.EmailHash == emailHash)),
            cancellationToken);
    }

    // Burada normalize e-postası eşleşen tüm sahipsiz guest siparişleri takipli getiriyorum.
    public async Task<IReadOnlyList<Order>> GetUnclaimedOrdersByEmailForUpdateAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default) =>
        await CreateOrderGraphQuery()
            .Where(order => !order.UserId.HasValue && order.CustomerSnapshot != null && order.CustomerSnapshot.Email == normalizedEmail)
            .OrderBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

    // Burada claim edilecek siparişlerin iade kayıtlarını takipli getiriyorum.
    public async Task<IReadOnlyList<ReturnRequest>> GetReturnsForOrdersForUpdateAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default) =>
        await _context.ReturnRequests.Where(request => orderIds.Contains(request.OrderId)).ToListAsync(cancellationToken);

    // Burada claim edilecek siparişlerin kupon kullanımlarını takipli getiriyorum.
    public async Task<IReadOnlyList<CouponUsage>> GetCouponUsagesForOrdersForUpdateAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default) =>
        await _context.CouponUsages.Where(usage => usage.OrderId.HasValue && orderIds.Contains(usage.OrderId.Value)).ToListAsync(cancellationToken);

    // Burada claim edilecek siparişlerin guest erişim grant'lerini takipli getiriyorum.
    public async Task<IReadOnlyList<GuestOrderAccessGrant>> GetAccessGrantsForOrdersForUpdateAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default) =>
        await _context.GuestOrderAccessGrants.Where(grant => orderIds.Contains(grant.OrderId)).ToListAsync(cancellationToken);

    // Burada claim edilecek siparişlerin magic-link kayıtlarını takipli getiriyorum.
    public async Task<IReadOnlyList<GuestOrderMagicLink>> GetMagicLinksForOrdersForUpdateAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default) =>
        await _context.GuestOrderMagicLinks.Where(link => orderIds.Contains(link.OrderId)).ToListAsync(cancellationToken);

    // Burada guest detay ve mutasyonları için gereken sipariş aggregate grafiğini oluşturuyorum.
    private IQueryable<Order> CreateOrderGraphQuery()
    {
        return _context.Orders
            .Include(order => order.Items)
            .Include(order => order.Payments)
            .Include(order => order.AddressSnapshots)
            .Include(order => order.CustomerSnapshot)
            .AsSplitQuery();
    }
}
