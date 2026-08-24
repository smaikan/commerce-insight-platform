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
        string conversationId,
        CancellationToken cancellationToken = default);

    // Burada terk edilmiş siparişe geç ulaşan kesin tahsilatı provider payment kimliğiyle geri çeviriyorum.
    Task<LatePaymentReversalResult> ReverseLatePaymentAsync(
        string providerPaymentId,
        string conversationId,
        decimal expectedAmount,
        CancellationToken cancellationToken = default);

    // Burada tahsil edilmiş ödemenin güncel cancel/refund durumunu reporting servisinden doğruluyorum.
    Task<PaymentReversalReport> RetrieveReversalReportAsync(
        string providerPaymentId,
        CancellationToken cancellationToken = default);

    // Burada provider paymentId üzerinden aynı gün tam iptal isteğini gönderiyorum.
    Task<PaymentReversalGatewayResult> CancelPaymentAsync(
        string providerPaymentId,
        string conversationId,
        decimal expectedPaidAmount,
        CancellationToken cancellationToken = default);

    // Burada standart item-level refund isteğini gerçek paymentTransactionId ve paidPrice tutarıyla gönderiyorum.
    Task<PaymentReversalGatewayResult> RefundPaymentItemAsync(
        string providerPaymentId,
        string providerPaymentTransactionId,
        string conversationId,
        decimal amount,
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
    string? FailureReason,
    bool IsDefinitiveFailure = false,
    string? ConversationId = null);

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
    int? InstallmentCount,
    string? ProviderPaymentId,
    int? FraudStatus,
    string? FailureReason,
    IReadOnlyList<CheckoutFormItemTransaction>? ItemTransactions = null);

public sealed record CheckoutFormItemTransaction(
    string ProviderPaymentTransactionId,
    string ItemId,
    decimal Price,
    decimal PaidPrice,
    int TransactionStatus);

public sealed record CheckoutFormWebhookNotification(
    string EventType,
    string ProviderPaymentId,
    string Token,
    string PaymentConversationId,
    string Status);

public sealed record LatePaymentReversalResult(
    bool Succeeded,
    bool Retryable,
    string? FailureReason = null);

public sealed record PaymentReversalGatewayResult(
    bool Succeeded,
    bool Retryable,
    string? ErrorCode = null,
    string? FailureReason = null);

public sealed record PaymentReversalReport(
    string ProviderPaymentId,
    string PaymentConversationId,
    string Currency,
    decimal Price,
    decimal PaidPrice,
    string RefundStatus,
    IReadOnlyList<PaymentReversalReportCancel> Cancels,
    IReadOnlyList<PaymentReversalReportItem> Items);

public sealed record PaymentReversalReportCancel(
    string ConversationId,
    decimal Amount,
    int Status,
    string Currency);

public sealed record PaymentReversalReportItem(
    string ProviderPaymentTransactionId,
    decimal Price,
    decimal PaidPrice,
    IReadOnlyList<PaymentReversalReportRefund> Refunds);

public sealed record PaymentReversalReportRefund(
    string ConversationId,
    decimal Amount,
    int Status,
    string Currency);
