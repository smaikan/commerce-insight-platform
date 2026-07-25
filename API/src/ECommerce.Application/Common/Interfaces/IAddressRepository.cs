using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Common.Interfaces;

public interface IAddressRepository
{
    // Burada yeni adres kaydını aynı iş birimi içinde takibe ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(Address address, CancellationToken cancellationToken = default);

    // Burada adresi silinmek üzere veritabanı takibinden kaldırma sözleşmesini tanımlıyorum.
    void Remove(Address address);

    // Burada adres listesini yalnız sahibi için takip etmeden okuma sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<Address>> GetByUserIdAsync(
        long userId,
        AddressType? type = null,
        CancellationToken cancellationToken = default);

    // Burada adresi yalnız sahibi için takip etmeden tekil okuma sözleşmesini tanımlıyorum.
    Task<Address?> GetByIdForUserAsync(
        Guid addressId,
        long userId,
        CancellationToken cancellationToken = default);

    // Burada adresi yalnız sahibi için değişiklik takibiyle tekil getirme sözleşmesini tanımlıyorum.
    Task<Address?> GetByIdForUserForUpdateAsync(
        Guid addressId,
        long userId,
        CancellationToken cancellationToken = default);

    // Burada varsayılan seçim değişirken aynı kullanıcı ve türdeki diğer adresleri takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<Address>> GetDefaultsForUserAndTypeForUpdateAsync(
        long userId,
        AddressType type,
        Guid? excludedAddressId = null,
        CancellationToken cancellationToken = default);

    // Burada adresin silinmesini engelleyen geçmiş sipariş bağı olup olmadığını denetleme sözleşmesini tanımlıyorum.
    Task<bool> IsReferencedByOrderAsync(
        Guid addressId,
        CancellationToken cancellationToken = default);
}
