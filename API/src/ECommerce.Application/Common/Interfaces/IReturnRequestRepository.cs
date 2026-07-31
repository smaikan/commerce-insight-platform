using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IReturnRequestRepository
{
    // Burada yeni iade veya değişim aggregate'ını kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(ReturnRequest returnRequest, CancellationToken cancellationToken = default);

    // Burada iade talebini yalnız gerçek sahibi için detay grafiğiyle takip etmeden getirme sözleşmesini tanımlıyorum.
    Task<ReturnRequest?> GetByIdForUserAsync(
        Guid returnRequestId,
        long userId,
        CancellationToken cancellationToken = default);

    // Burada iade talebini yönetim detay ekranı için takip etmeden getirme sözleşmesini tanımlıyorum.
    Task<ReturnRequest?> GetByIdAsync(Guid returnRequestId, CancellationToken cancellationToken = default);

    // Burada iade talebini yalnız sahibi için değişiklik akışında takipli getirme sözleşmesini tanımlıyorum.
    Task<ReturnRequest?> GetByIdForUserForUpdateAsync(
        Guid returnRequestId,
        long userId,
        CancellationToken cancellationToken = default);

    // Burada iade talebini yönetim iş akışında takipli getirme sözleşmesini tanımlıyorum.
    Task<ReturnRequest?> GetByIdForUpdateAsync(Guid returnRequestId, CancellationToken cancellationToken = default);

    // Burada aynı siparişteki daha önceki iade adetlerini serializable işlem içinde takipli getiriyorum.
    Task<IReadOnlyList<ReturnRequest>> GetByOrderIdForUpdateAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    // Burada kullanıcının kendi iade taleplerini sayfalı ve owner filtresiyle getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<ReturnRequest>> GetListForUserAsync(
        ReturnRequestListFilter filter,
        long userId,
        CancellationToken cancellationToken = default);

    // Burada yönetim ekranındaki iade taleplerini güvenli filtrelerle sayfalama sözleşmesini tanımlıyorum.
    Task<PagedResult<ReturnRequest>> GetListAsync(
        ReturnRequestListFilter filter,
        CancellationToken cancellationToken = default);
}
