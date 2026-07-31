using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IShippingMethodRepository
{
    // Burada yeni kargo yöntemini kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(ShippingMethod shippingMethod, CancellationToken cancellationToken = default);

    // Burada kargo yöntemini kimliğiyle takip etmeden okuma sözleşmesini tanımlıyorum.
    Task<ShippingMethod?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada kargo yöntemini güncelleme için takipli getirme sözleşmesini tanımlıyorum.
    Task<ShippingMethod?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada kargo yöntemlerini sayfalama ve isteğe bağlı aktiflik filtresiyle okuma sözleşmesini tanımlıyorum.
    Task<PagedResult<ShippingMethod>> GetListAsync(
        int pageNumber,
        int pageSize,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    // Burada kargo yöntemi adının başka bir kayıtta kullanılıp kullanılmadığını denetleme sözleşmesini tanımlıyorum.
    Task<bool> NameExistsAsync(
        string name,
        Guid? excludedShippingMethodId = null,
        CancellationToken cancellationToken = default);

    // Burada checkout seçimi için kargo yönteminin aktif olarak bulunup bulunmadığını denetleme sözleşmesini tanımlıyorum.
    Task<bool> ExistsAndActiveAsync(Guid id, CancellationToken cancellationToken = default);
}
