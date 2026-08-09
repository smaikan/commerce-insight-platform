using System.Globalization;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.GuestOrders.Checkout;

public sealed class CreateGuestOrderCommandHandler : IRequestHandler<CreateGuestOrderCommand, GuestCheckoutResult>
{
    private static readonly TimeSpan IdempotencyLifetime = TimeSpan.FromHours(24);
    private readonly OrderCheckoutOrchestrator _checkout;
    private readonly IGuestOrderRepository _guestOrders;
    private readonly IEmailOutboxRepository _outbox;
    private readonly IGuestTokenService _tokens;
    private readonly IGuestOrderAccessTokenProtector _protector;
    private readonly IGuestCheckoutProtectionService _protection;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    // Burada guest checkout'a özgü session, idempotency, magic-link ve transaction bağımlılıklarını hazırlıyorum.
    public CreateGuestOrderCommandHandler(
        OrderCheckoutOrchestrator checkout,
        IGuestOrderRepository guestOrders,
        IEmailOutboxRepository outbox,
        IGuestTokenService tokens,
        IGuestOrderAccessTokenProtector protector,
        IGuestCheckoutProtectionService protection,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork)
    {
        _checkout = checkout;
        _guestOrders = guestOrders;
        _outbox = outbox;
        _tokens = tokens;
        _protector = protector;
        _protection = protection;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    // Burada guest siparişi bütün erişim ve outbox kayıtlarıyla serializable transaction içinde oluşturuyorum.
    public Task<GuestCheckoutResult> Handle(CreateGuestOrderCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            token => CreateInTransactionAsync(request, token),
            cancellationToken);
    }

    // Burada idempotent tekrar, session yetkisi ve ortak checkout sonucunu aynı transaction'a bağlıyorum.
    private async Task<GuestCheckoutResult> CreateInTransactionAsync(
        CreateGuestOrderCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var cartSessionHash = _tokens.Hash(request.CartSessionId);
        var keyHash = _tokens.Hash(request.IdempotencyKey);
        var requestHash = _tokens.Hash(CreateRequestFingerprint(request));
        var existing = await _guestOrders.GetIdempotencyForUpdateAsync(cartSessionHash, keyHash, cancellationToken);
        if (existing is not null && existing.ExpiresAt > now)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
            {
                throw new ApiContractException(409, "idempotency_key_reused", "Idempotency conflict", "Idempotency-Key farklı bir checkout isteğiyle daha önce kullanıldı.");
            }

            if (existing.Order.UserId.HasValue)
            {
                throw new ConflictException("Guest order has already been claimed.");
            }

            var replaySession = await ResolveSessionAsync(request.ExistingOrderSessionToken, now, cancellationToken);
            var replayGrant = await _guestOrders.GetAccessGrantForUpdateAsync(
                replaySession.Session.Id, existing.OrderId, cancellationToken);
            if (replayGrant is null)
            {
                await _guestOrders.AddAccessGrantAsync(
                    new GuestOrderAccessGrant(replaySession.Session.Id, existing.OrderId, now), cancellationToken);
            }
            else if (replayGrant.RevokedAt.HasValue)
            {
                replayGrant.Reactivate(now);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new GuestCheckoutResult(
                existing.Order.ToDto(),
                replaySession.NewSessionToken,
                replaySession.NewCsrfToken,
                replaySession.NewSessionToken is null ? null : replaySession.Session.ExpiresAt,
                true);
        }

        await _protection.EvaluateCheckoutAsync(
            new GuestCheckoutProtectionRequest(
                request.IpAddress,
                request.CartSessionId,
                request.Customer.Email.Trim().ToLowerInvariant(),
                request.TurnstileToken),
            cancellationToken);

        var sessionResolution = await ResolveSessionAsync(request.ExistingOrderSessionToken, now, cancellationToken);
        var emailHash = _tokens.Hash(request.Customer.Email.Trim().ToLowerInvariant());
        var activeReservationCount = await _guestOrders.CountActiveUnpaidOrdersAsync(
            sessionResolution.Session.Id, emailHash, now, cancellationToken);
        if (activeReservationCount >= 3)
        {
            throw new ApiContractException(429, "guest_checkout_rate_limited", "Guest checkout rate limited", "En fazla üç aktif ödenmemiş guest sipariş rezervasyonu oluşturabilirsiniz.");
        }

        var order = await _checkout.CreateAsync(
            new OrderCheckoutInput(
                CartOwner.ForGuest(request.CartSessionId),
                null,
                request.ExpectedCartConcurrencyToken,
                request.ShippingMethodId,
                request.CouponCode,
                true,
                GuestCustomer: request.Customer,
                GuestShippingAddress: request.ShippingAddress with { SourceAddressId = null },
                GuestBillingAddress: request.BillingAddress is null
                    ? null
                    : request.BillingAddress with { SourceAddressId = null }),
            cancellationToken);

        var magicToken = _tokens.CreateToken();
        var magicExpiry = now.Add(GuestOrderAccessPolicy.MagicLinkLifetime);
        await _guestOrders.AddAccessGrantAsync(
            new GuestOrderAccessGrant(sessionResolution.Session.Id, order.Id, now), cancellationToken);
        await _guestOrders.AddMagicLinkAsync(
            new GuestOrderMagicLink(order.Id, magicToken.Hash, emailHash, now, magicExpiry), cancellationToken);
        if (existing is null)
        {
            await _guestOrders.AddIdempotencyAsync(
                new GuestCheckoutIdempotency(
                    cartSessionHash, keyHash, requestHash, order, now, now.Add(IdempotencyLifetime)),
                cancellationToken);
        }
        else
        {
            existing.ReplaceExpiredResult(requestHash, order, now, now.Add(IdempotencyLifetime));
        }
        await _outbox.AddAsync(
            EmailOutboxMessage.CreateGuestOrderAccess(
                request.Customer.Email,
                $"{request.Customer.FirstName.Trim()} {request.Customer.LastName.Trim()}",
                order.OrderNumber,
                _protector.Protect(magicToken.RawValue),
                magicExpiry,
                now),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new GuestCheckoutResult(
            order.ToDto(),
            sessionResolution.NewSessionToken,
            sessionResolution.NewCsrfToken,
            sessionResolution.NewSessionToken is null ? null : sessionResolution.Session.ExpiresAt,
            false);
    }

    // Burada geçerli mevcut session'ı kullanıyor veya yeni 256 bit session ve CSRF değerleri üretiyorum.
    private async Task<SessionResolution> ResolveSessionAsync(
        string? existingRawToken,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(existingRawToken))
        {
            var existing = await _guestOrders.GetSessionForUpdateAsync(_tokens.Hash(existingRawToken), cancellationToken);
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

    // Burada idempotency için PII değerlerini kalıcılaştırmadan deterministik istek parmak izi üretiyorum.
    private static string CreateRequestFingerprint(CreateGuestOrderCommand request)
    {
        return string.Join('|',
            request.ExpectedCartConcurrencyToken.ToString("N"),
            request.Customer.FirstName.Trim(),
            request.Customer.LastName.Trim(),
            request.Customer.Email.Trim().ToLowerInvariant(),
            request.Customer.PhoneNumber.Trim(),
            FormatAddress(request.ShippingAddress),
            request.BillingAddress is null ? "billing=fallback" : FormatAddress(request.BillingAddress),
            request.ShippingMethodId.ToString("N"),
            request.CouponCode?.Trim().ToUpperInvariant() ?? string.Empty);
    }

    // Burada adres alanlarını idempotency parmak izi için sıralı ve kültürden bağımsız biçime dönüştürüyorum.
    private static string FormatAddress(CheckoutAddressInput address)
    {
        return string.Join('~',
            ((int)address.Type).ToString(CultureInfo.InvariantCulture),
            address.Title.Trim(), address.FirstName.Trim(), address.LastName.Trim(),
            address.PhoneNumber.Trim(), address.City.Trim(), address.District.Trim(),
            address.FullAddress.Trim(), address.PostalCode?.Trim() ?? string.Empty);
    }

    // Burada çözülen session ile yalnız yeni üretimde API'ye dönecek ham cookie değerlerini taşıyorum.
    private sealed record SessionResolution(GuestOrderSession Session, string? NewSessionToken, string? NewCsrfToken);
}
