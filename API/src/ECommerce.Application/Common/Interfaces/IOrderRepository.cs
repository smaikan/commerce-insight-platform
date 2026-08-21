using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IOrderRepository
{
    // Burada yeni sipariş aggregate'ını veritabanı takibine ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    // Burada mevcut siparişe ait yeni ödeme denemesini açıkça veritabanı takibine ekleme sözleşmesini tanımlıyorum.
    Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default);

    // Burada siparişi sahibi ve kalemleriyle birlikte okuma amaçlı getirme sözleşmesini tanımlıyorum.
    Task<Order?> GetByIdForUserAsync(Guid orderId, long userId, CancellationToken cancellationToken = default);

    // Burada siparişi yönetim detay ekranı için takip etmeden ilişkileriyle getirme sözleşmesini tanımlıyorum.
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    // Burada dış sistem sipariş numarasıyla yapılan tekrar güvenli içe aktarmayı çözümlemek için siparişi getiriyorum.
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);

    // Burada sipariş numarasını yalnız gerçek kullanıcı sahibi kapsamında çözmeyi tanımlıyorum.
    Task<Order?> GetByOrderNumberForUserAsync(string orderNumber, long userId, CancellationToken cancellationToken = default);

    // Burada kullanıcının kendi siparişini ödeme veya iptal için ilişkileriyle takipli getirme sözleşmesini tanımlıyorum.
    Task<Order?> GetByIdForUserForUpdateAsync(Guid orderId, long userId, CancellationToken cancellationToken = default);

    // Burada siparişi yönetim durum değişikliği için ilişkileriyle takipli getirme sözleşmesini tanımlıyorum.
    Task<Order?> GetByIdForUpdateAsync(Guid orderId, CancellationToken cancellationToken = default);

    // Burada sağlayıcı callback tokenına bağlı sipariş grafiğini güvenli sonuçlandırma için getiriyorum.
    Task<Order?> GetByPaymentProviderTokenAsync(
        string providerToken,
        bool forUpdate,
        CancellationToken cancellationToken = default);

    // Burada süresi geçmiş stok rezervasyonlarını sağlayıcı mutabakatından önce takip etmeden ve sınırlı getiriyorum.
    Task<IReadOnlyList<Order>> GetExpiredStockReservationsAsync(
        DateTime utcNow,
        int maxCount,
        CancellationToken cancellationToken = default);

    // Burada kullanıcının kendi siparişlerini sayfalı ve takip edilmeden getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<Order>> GetListForUserAsync(
        OrderListFilter filter,
        long userId,
        CancellationToken cancellationToken = default);

    // Burada yönetim ekranındaki siparişleri sayfalı ve takip edilmeden getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<Order>> GetListAsync(OrderListFilter filter, CancellationToken cancellationToken = default);
}
