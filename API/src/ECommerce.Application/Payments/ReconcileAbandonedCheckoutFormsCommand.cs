using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using MediatR;

namespace ECommerce.Application.Payments;

public sealed record ReconcileAbandonedCheckoutFormsCommand(int BatchSize)
    : IRequest<AbandonedCheckoutReconciliationResult>;

public sealed record AbandonedCheckoutReconciliationResult(
    int CandidateCount,
    int CompletedCount);

public sealed class ReconcileAbandonedCheckoutFormsCommandHandler
    : IRequestHandler<ReconcileAbandonedCheckoutFormsCommand, AbandonedCheckoutReconciliationResult>
{
    private readonly IOrderRepository _orders;
    private readonly CheckoutFormPaymentService _payments;
    private readonly IDateTimeProvider _clock;

    // Burada terk edilmiş ödeme tokenlarını bounded okuyup ortak iyzico uzlaştırma servisine taşıyan bağımlılıkları hazırlıyorum.
    public ReconcileAbandonedCheckoutFormsCommandHandler(
        IOrderRepository orders,
        CheckoutFormPaymentService payments,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _payments = payments;
        _clock = clock;
    }

    // Burada zamanı gelen terk edilmiş tokenları kararlı sırayla uzlaştırıp tamamlanan sayısını raporluyorum.
    public async Task<AbandonedCheckoutReconciliationResult> Handle(
        ReconcileAbandonedCheckoutFormsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BatchSize is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(request.BatchSize));
        }

        var tokens = await _orders.GetDueAbandonedPaymentTokensAsync(
            _clock.UtcNow,
            request.BatchSize,
            cancellationToken);
        var completedCount = 0;
        foreach (var token in tokens)
        {
            if (await _payments.ReconcileAbandonedCheckoutFormAsync(token, cancellationToken))
            {
                completedCount++;
            }
        }

        return new AbandonedCheckoutReconciliationResult(tokens.Count, completedCount);
    }
}
