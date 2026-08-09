using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.GuestOrders;

public sealed class GuestOrderAccessService
{
    private readonly IGuestOrderRepository _guestOrders;
    private readonly IEmailOutboxRepository _outbox;
    private readonly IUserRepository _users;
    private readonly IGuestTokenService _tokens;
    private readonly IGuestOrderAccessTokenProtector _protector;
    private readonly IGuestCheckoutProtectionService _protection;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Burada guest erişim linki, session exchange, listeleme ve claim bağımlılıklarını hazırlıyorum.
    public GuestOrderAccessService(
        IGuestOrderRepository guestOrders,
        IEmailOutboxRepository outbox,
        IUserRepository users,
        IGuestTokenService tokens,
        IGuestOrderAccessTokenProtector protector,
        IGuestCheckoutProtectionService protection,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork)
    {
        _guestOrders = guestOrders;
        _outbox = outbox;
        _users = users;
        _tokens = tokens;
        _protector = protector;
        _protection = protection;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    // Burada sipariş numarası ve e-postayı yetki vermeden yalnız magic-link gönderimi için kullanıyorum.
    public async Task RequestAccessLinkAsync(
        string orderNumber,
        string email,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedOrderNumber = orderNumber.Trim().ToUpperInvariant();
        var preflightOrder = await _guestOrders.GetUnclaimedOrderForAccessLinkAsync(
            normalizedOrderNumber,
            normalizedEmail,
            cancellationToken);
        await _protection.EvaluateMagicLinkRequestAsync(preflightOrder?.Id, ipAddress, cancellationToken);
        if (preflightOrder is null)
        {
            return;
        }

        await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async token =>
            {
                var order = await _guestOrders.GetUnclaimedOrderForAccessLinkAsync(
                    normalizedOrderNumber, normalizedEmail, token);
                if (order is null || order.CustomerSnapshot is null)
                {
                    return true;
                }

                var tokenPair = _tokens.CreateToken();
                var expiresAt = _clock.UtcNow.Add(GuestOrderAccessPolicy.MagicLinkLifetime);
                await _guestOrders.AddMagicLinkAsync(
                    new GuestOrderMagicLink(
                        order.Id,
                        tokenPair.Hash,
                        _tokens.Hash(normalizedEmail),
                        _clock.UtcNow,
                        expiresAt),
                    token);
                await _outbox.AddAsync(
                    EmailOutboxMessage.CreateGuestOrderAccess(
                        order.CustomerSnapshot.Email,
                        $"{order.CustomerSnapshot.FirstName} {order.CustomerSnapshot.LastName}",
                        order.OrderNumber,
                        _protector.Protect(tokenPair.RawValue),
                        expiresAt,
                        _clock.UtcNow),
                    token);
                await _unitOfWork.SaveChangesAsync(token);
                return true;
            },
            cancellationToken);
    }

    // Burada tek kullanımlık magic-link tokenını doğrulanmış guest session ve sipariş grant'ine çeviriyorum.
    public Task<GuestSessionExchangeResult> ExchangeAsync(
        string rawMagicToken,
        string? existingSessionToken,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            token => ExchangeInTransactionAsync(rawMagicToken, existingSessionToken, token),
            cancellationToken);
    }

    // Burada magic-link tüketimi, e-posta doğrulaması ve grant oluşturmayı atomik yapıyorum.
    private async Task<GuestSessionExchangeResult> ExchangeInTransactionAsync(
        string rawMagicToken,
        string? existingSessionToken,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var link = await _guestOrders.GetMagicLinkForUpdateAsync(_tokens.Hash(rawMagicToken), cancellationToken)
            ?? throw new ApiContractException(404, "invalid_guest_access", "Guest access not found", "Erişim bağlantısı geçersiz veya süresi dolmuş.");
        if (!link.IsActiveAt(now))
        {
            throw new ApiContractException(404, "invalid_guest_access", "Guest access not found", "Erişim bağlantısı geçersiz veya süresi dolmuş.");
        }

        var resolution = await ResolveOrCreateSessionAsync(existingSessionToken, now, cancellationToken);
        link.Consume(now);
        resolution.Session.VerifyEmail(link.EmailHash, now);
        var existingGrant = await _guestOrders.GetAccessGrantForUpdateAsync(
            resolution.Session.Id,
            link.OrderId,
            cancellationToken);
        if (existingGrant is null)
        {
            await _guestOrders.AddAccessGrantAsync(
                new GuestOrderAccessGrant(resolution.Session.Id, link.OrderId, now), cancellationToken);
        }
        else if (existingGrant.RevokedAt.HasValue)
        {
            existingGrant.Reactivate(now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new GuestSessionExchangeResult(
            link.OrderId,
            resolution.NewSessionToken,
            resolution.NewCsrfToken,
            resolution.Session.ExpiresAt);
    }

    // Burada guest session'ın erişebildiği siparişleri PII içermeyen özetlerle getiriyorum.
    public async Task<PagedResult<OrderSummaryDto>> GetOrdersAsync(
        string rawSessionToken,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAsync(rawSessionToken, null, false, cancellationToken);
        var orders = await _guestOrders.GetOrdersForSessionAsync(session.Id, pageNumber, pageSize, cancellationToken);
        return orders.Map(order => order.ToSummaryDto());
    }

    // Burada guest session'ın yalnız grant verilen sipariş detayını getiriyorum.
    public async Task<OrderDto> GetOrderAsync(
        string rawSessionToken,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAsync(rawSessionToken, null, false, cancellationToken);
        var order = await _guestOrders.GetOrderForSessionAsync(session.Id, orderId, false, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        return order.ToDto();
    }

    // Burada JWT hesabının e-postasıyla magic-link doğrulamasını eşleştirip tüm sahipsiz siparişleri atomik bağlıyorum.
    public Task<int> ClaimAsync(
        string rawSessionToken,
        string rawCsrfToken,
        long userId,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            token => ClaimInTransactionAsync(rawSessionToken, rawCsrfToken, userId, token),
            cancellationToken);
    }

    // Burada claim sırasında sipariş, iade, kupon ve guest erişim kayıtlarını birlikte güncelliyorum.
    private async Task<int> ClaimInTransactionAsync(
        string rawSessionToken,
        string rawCsrfToken,
        long userId,
        CancellationToken cancellationToken)
    {
        var session = await ValidateSessionAsync(rawSessionToken, rawCsrfToken, true, cancellationToken);
        var user = await _users.GetByIdForUpdateAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");
        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        var emailHash = _tokens.Hash(normalizedEmail);
        if (!string.Equals(session.VerifiedEmailHash, emailHash, StringComparison.Ordinal))
        {
            throw new ApiContractException(403, "invalid_guest_access", "Guest access forbidden", "Doğrulanmış guest e-postası hesap e-postasıyla eşleşmiyor.");
        }

        var orders = await _guestOrders.GetUnclaimedOrdersByEmailForUpdateAsync(normalizedEmail, cancellationToken);
        var orderIds = orders.Select(order => order.Id).ToList();
        if (orderIds.Count == 0)
        {
            return 0;
        }

        foreach (var order in orders)
        {
            order.Claim(userId);
        }

        foreach (var request in await _guestOrders.GetReturnsForOrdersForUpdateAsync(orderIds, cancellationToken))
        {
            request.AssignToUser(userId);
        }

        foreach (var usage in await _guestOrders.GetCouponUsagesForOrdersForUpdateAsync(orderIds, cancellationToken))
        {
            usage.AssignToUser(userId);
        }

        var now = _clock.UtcNow;
        foreach (var grant in await _guestOrders.GetAccessGrantsForOrdersForUpdateAsync(orderIds, cancellationToken))
        {
            grant.Revoke(now);
        }

        foreach (var link in await _guestOrders.GetMagicLinksForOrdersForUpdateAsync(orderIds, cancellationToken))
        {
            link.Revoke(now);
        }

        session.Revoke(now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return orders.Count;
    }

    // Burada session tokenını ve mutasyonlarda double-submit CSRF değerini hash üzerinden doğruluyorum.
    public async Task<GuestOrderSession> ValidateSessionAsync(
        string rawSessionToken,
        string? rawCsrfToken,
        bool requireCsrf,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawSessionToken))
        {
            throw new ApiContractException(401, "invalid_guest_access", "Guest access required", "Geçerli guest sipariş oturumu gereklidir.");
        }

        var session = await _guestOrders.GetSessionForUpdateAsync(_tokens.Hash(rawSessionToken), cancellationToken);
        if (session is null || !session.IsActiveAt(_clock.UtcNow))
        {
            throw new ApiContractException(401, "invalid_guest_access", "Guest access required", "Guest sipariş oturumu geçersiz veya süresi dolmuş.");
        }

        if (requireCsrf && (string.IsNullOrWhiteSpace(rawCsrfToken) ||
            !string.Equals(session.CsrfTokenHash, _tokens.Hash(rawCsrfToken), StringComparison.Ordinal)))
        {
            throw new ApiContractException(403, "invalid_guest_access", "CSRF validation failed", "Guest mutasyonu için CSRF doğrulaması başarısız oldu.");
        }

        session.Touch(_clock.UtcNow);
        return session;
    }

    // Burada mevcut aktif session'ı çözüyor veya exchange için yeni 256 bit session üretiyorum.
    private async Task<SessionResolution> ResolveOrCreateSessionAsync(
        string? existingSessionToken,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(existingSessionToken))
        {
            var existing = await _guestOrders.GetSessionForUpdateAsync(_tokens.Hash(existingSessionToken), cancellationToken);
            if (existing is not null && existing.IsActiveAt(now))
            {
                existing.Touch(now);
                return new SessionResolution(existing, null, null);
            }
        }

        var sessionToken = _tokens.CreateToken();
        var csrfToken = _tokens.CreateToken();
        var session = new GuestOrderSession(sessionToken.Hash, csrfToken.Hash, now, now.Add(GuestOrderAccessPolicy.SessionLifetime));
        await _guestOrders.AddSessionAsync(session, cancellationToken);
        return new SessionResolution(session, sessionToken.RawValue, csrfToken.RawValue);
    }

    // Burada session ve yalnız yeni üretimde dönen ham cookie değerlerini taşıyorum.
    private sealed record SessionResolution(GuestOrderSession Session, string? NewSessionToken, string? NewCsrfToken);
}

// Burada magic-link exchange sonrasında API'nin cookie yazması için gereken sonucu taşıyorum.
public sealed record GuestSessionExchangeResult(
    Guid OrderId,
    string? NewSessionToken,
    string? NewCsrfToken,
    DateTime SessionExpiresAt);
