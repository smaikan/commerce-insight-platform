using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Payments;

public interface IPaymentGateway
{
    // Burada adapter'ın desteklediği ödeme sağlayıcısını bildiriyorum.
    PaymentProvider Provider { get; }

    // Burada güvenilir sipariş tutarını sağlayıcıya ödeme denemesi olarak iletme sözleşmesini tanımlıyorum.
    Task<PaymentGatewayResult> ChargeAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);
}

public interface IPaymentGatewayReconciler
{
    // Burada sağlayıcının çözümleyebildiği ödeme türünü bildiriyorum.
    PaymentProvider Provider { get; }

    // Burada süresi dolmuş bekleyen denemenin sağlayıcıda ödenmiş mi yoksa güvenle iptal edilmiş mi olduğunu çözümlüyorum.
    Task<PaymentReconciliationResult> ReconcilePendingPaymentAsync(
        PaymentReconciliationRequest request,
        CancellationToken cancellationToken = default);
}

// Burada ödeme adapter'ına yalnız sunucunun hesapladığı sipariş bilgilerini taşıyorum.
public sealed record PaymentGatewayRequest(
    Guid OrderId,
    decimal Amount,
    string IdempotencyKey);

// Burada sağlayıcı adapter'ının istemciye sızdırılmayacak normalize sonucunu tanımlıyorum.
public sealed record PaymentGatewayResult(
    bool Succeeded,
    string? TransactionId,
    string? FailureReason);

// Burada sağlayıcı mutabakatı için yalnız sunucunun bildiği ödeme denemesi bilgilerini taşıyorum.
public sealed record PaymentReconciliationRequest(
    Guid OrderId,
    Guid PaymentId,
    decimal BasketPrice,
    decimal Amount,
    string IdempotencyKey,
    string? ProviderToken = null);

// Burada sağlayıcının güvenli mutabakat sonucunun olası durumlarını tanımlıyorum.
public enum PaymentReconciliationStatus
{
    Paid = 0,
    Cancelled = 1,
    Unknown = 2
}

// Burada sağlayıcının bekleyen ödeme için kesin ya da belirsiz mutabakat sonucunu taşıyorum.
public sealed record PaymentReconciliationResult(
    PaymentReconciliationStatus Status,
    string? TransactionId = null,
    decimal? ProviderPaidAmount = null,
    int? InstallmentCount = null,
    int? FraudStatus = null);
