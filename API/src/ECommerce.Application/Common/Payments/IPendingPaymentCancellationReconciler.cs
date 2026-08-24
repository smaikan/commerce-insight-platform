using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Payments;

public interface IPendingPaymentCancellationReconciler
{
    // Burada sahipliği doğrulanmış siparişteki bekleyen hosted ödemeyi iptal kararından önce sağlayıcıyla uzlaştırıyorum.
    Task<PaymentStatus?> ReconcileForCancellationAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
