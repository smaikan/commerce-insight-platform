using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IGuestOrderRepository
{
    // Burada yeni guest session, erişim, magic-link ve idempotency kayıtlarını takibe ekleme sözleşmesini tanımlıyorum.
    Task AddSessionAsync(GuestOrderSession session, CancellationToken cancellationToken = default);
    Task AddAccessGrantAsync(GuestOrderAccessGrant grant, CancellationToken cancellationToken = default);
    Task AddMagicLinkAsync(GuestOrderMagicLink link, CancellationToken cancellationToken = default);
    Task AddIdempotencyAsync(GuestCheckoutIdempotency record, CancellationToken cancellationToken = default);

    // Burada hash ile guest session ve magic-link kayıtlarını güvenlik güncellemesi için getiriyorum.
    Task<GuestOrderSession?> GetSessionForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<GuestOrderMagicLink?> GetMagicLinkForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<Order?> GetUnclaimedOrderForAccessLinkAsync(string orderNumber, string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> HasActiveAccessGrantAsync(Guid sessionId, Guid orderId, CancellationToken cancellationToken = default);
    Task<GuestOrderAccessGrant?> GetAccessGrantForUpdateAsync(Guid sessionId, Guid orderId, CancellationToken cancellationToken = default);

    // Burada session yetkisine göre sipariş ve iade erişimini 404 semantiğine uygun çözüyorum.
    Task<Order?> GetOrderForSessionAsync(Guid sessionId, Guid orderId, bool forUpdate, CancellationToken cancellationToken = default);
    Task<ReturnRequest?> GetReturnForSessionAsync(Guid sessionId, Guid orderId, Guid returnId, CancellationToken cancellationToken = default);
    Task<PagedResult<Order>> GetOrdersForSessionAsync(Guid sessionId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ReturnRequest>> GetReturnsForSessionOrderAsync(Guid sessionId, Guid orderId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Burada checkout tekrar kaydını ve guest korunma sayaçlarında gereken verileri getiriyorum.
    Task<GuestCheckoutIdempotency?> GetIdempotencyForUpdateAsync(string cartSessionHash, string keyHash, CancellationToken cancellationToken = default);
    Task<int> CountActiveUnpaidOrdersAsync(Guid sessionId, string emailHash, DateTime utcNow, CancellationToken cancellationToken = default);

    // Burada claim sırasında aynı e-postadaki sahipsiz sipariş ve bağlı kayıtları atomik güncelleme için getiriyorum.
    Task<IReadOnlyList<Order>> GetUnclaimedOrdersByEmailForUpdateAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReturnRequest>> GetReturnsForOrdersForUpdateAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CouponUsage>> GetCouponUsagesForOrdersForUpdateAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuestOrderAccessGrant>> GetAccessGrantsForOrdersForUpdateAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GuestOrderMagicLink>> GetMagicLinksForOrdersForUpdateAsync(IReadOnlyCollection<Guid> orderIds, CancellationToken cancellationToken = default);
}
