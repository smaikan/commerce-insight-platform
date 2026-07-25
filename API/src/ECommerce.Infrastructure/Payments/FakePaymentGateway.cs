using ECommerce.Application.Common.Payments;
using ECommerce.Domain.Enums;
using Microsoft.Extensions.Hosting;

namespace ECommerce.Infrastructure.Payments;

// Burada gerçek kart verisi işlemeyen, geliştirme ve test için güvenli sahte ödeme adapter'ını tanımlıyorum.
public sealed class FakePaymentGateway : IPaymentGateway, IPaymentGatewayReconciler
{
    private readonly IHostEnvironment _environment;

    // Burada sahte sağlayıcının yalnız geliştirme/test ortamlarında ödeme başarısı üretebilmesi için ortam bilgisini hazırlıyorum.
    public FakePaymentGateway(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public PaymentProvider Provider => PaymentProvider.Fake;

    // Burada güvenilir tutar için deterministik olmayan fakat benzersiz bir test işlem kimliği üretiyorum.
    public Task<PaymentGatewayResult> ChargeAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_environment.IsDevelopment() && !_environment.IsEnvironment("Testing"))
        {
            return Task.FromResult(new PaymentGatewayResult(
                false,
                null,
                "Fake payment provider is disabled outside development and testing."));
        }

        var transactionId = $"fake_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentGatewayResult(true, transactionId, null));
    }

    // Burada sahte sağlayıcının arka planda devam eden tahsilatı olmadığı için zaman aşımındaki denemeyi güvenle iptal edilmiş sayıyorum.
    public Task<PaymentReconciliationResult> ReconcilePendingPaymentAsync(
        PaymentReconciliationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentReconciliationResult(PaymentReconciliationStatus.Cancelled));
    }
}
