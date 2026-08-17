using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Payments;

public interface ICheckoutFormGateway
{
    // Burada adapter'ın desteklediği hosted ödeme formu sağlayıcısını bildiriyorum.
    PaymentProvider Provider { get; }

    // Burada sağlayıcının eksiksiz configuration ile kullanıma açık olup olmadığını bildiriyorum.
    bool IsEnabled { get; }

    // Burada güvenilir sipariş snapshot'ından sağlayıcı ödeme formu oturumu oluşturuyorum.
    Task<CheckoutFormInitializeResult> InitializeAsync(
        CheckoutFormInitializeGatewayRequest request,
        CancellationToken cancellationToken = default);

    // Burada callback tokenıyla ödeme sonucunu sağlayıcıdan yeniden sorguluyorum.
    Task<CheckoutFormRetrieveResult> RetrieveAsync(
        string token,
        CancellationToken cancellationToken = default);

    // Burada webhook gövdesinin sağlayıcı imzasını sabit zamanlı karşılaştırmayla doğruluyorum.
    bool ValidateWebhookSignature(CheckoutFormWebhookNotification notification, string signature);
}

public sealed record CheckoutFormInitializeGatewayRequest(
    Guid PaymentId,
    Guid OrderId,
    string ConversationId,
    string BasketId,
    decimal Price,
    decimal PaidPrice,
    string ClientIpAddress,
    CheckoutFormBuyer Buyer,
    CheckoutFormAddress BillingAddress,
    CheckoutFormAddress ShippingAddress,
    IReadOnlyList<CheckoutFormBasketItem> Items);

public sealed record CheckoutFormBuyer(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber);

public sealed record CheckoutFormAddress(
    string ContactName,
    string City,
    string District,
    string FullAddress,
    string? PostalCode);

public sealed record CheckoutFormBasketItem(
    string Id,
    string Name,
    decimal Price);

public sealed record CheckoutFormInitializeResult(
    bool Succeeded,
    string? Token,
    string? PaymentPageUrl,
    DateTime? ExpiresAt,
    string? FailureReason);

public enum CheckoutFormPaymentState
{
    Pending = 0,
    Paid = 1,
    Failed = 2
}

public sealed record CheckoutFormRetrieveResult(
    CheckoutFormPaymentState State,
    string Token,
    string ConversationId,
    string BasketId,
    string Currency,
    decimal Price,
    decimal PaidPrice,
    string? ProviderPaymentId,
    int? FraudStatus,
    string? FailureReason);

public sealed record CheckoutFormWebhookNotification(
    string EventType,
    string ProviderPaymentId,
    string Token,
    string PaymentConversationId,
    string Status);
