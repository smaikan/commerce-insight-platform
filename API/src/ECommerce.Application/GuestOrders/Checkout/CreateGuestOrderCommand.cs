using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using MediatR;

namespace ECommerce.Application.GuestOrders.Checkout;

// Burada guest checkout için yalnız müşteri, adres, kargo, kupon ve güvenlik girdilerini taşıyorum.
public sealed record CreateGuestOrderCommand(
    string CartSessionId,
    string? ExistingOrderSessionToken,
    string IpAddress,
    string? TurnstileToken,
    string IdempotencyKey,
    Guid ExpectedCartConcurrencyToken,
    CheckoutCustomerInput Customer,
    CheckoutAddressInput ShippingAddress,
    CheckoutAddressInput? BillingAddress,
    Guid ShippingMethodId,
    string? CouponCode) : IRequest<GuestCheckoutResult>;

// Burada API'nin güvenli cookie yazması için geçici session değerleriyle sipariş cevabını ayırıyorum.
public sealed record GuestCheckoutResult(
    OrderDto Order,
    string? NewSessionToken,
    string? NewCsrfToken,
    DateTime? SessionExpiresAt,
    bool WasReplay);
