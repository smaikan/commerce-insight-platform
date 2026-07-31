using MediatR;

namespace ECommerce.Application.Orders.Commands.ExpireStockReservations;

// Burada süre dolan stok rezervasyonlarının sınırlı bir parti halinde sonlandırılması isteğini taşıyorum.
public sealed record ExpireStockReservationsCommand(int BatchSize = 100) : IRequest<StockReservationExpirationResult>;

// Burada rezervasyon sonlandırma partisinin güvenli iptal, mutabakat bekleme ve ödenmiş sonuç özetini taşıyorum.
public sealed record StockReservationExpirationResult(
    int CancelledOrderCount,
    int SkippedPendingPaymentCount,
    int ReconciledPaidOrderCount = 0);
