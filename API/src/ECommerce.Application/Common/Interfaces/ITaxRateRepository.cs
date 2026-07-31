using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface ITaxRateRepository
{
    // Burada yeni vergi oranını kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(TaxRate taxRate, CancellationToken cancellationToken = default);

    // Burada vergi oranını kimliğiyle takip etmeden okuma sözleşmesini tanımlıyorum.
    Task<TaxRate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada vergi oranını güncelleme için takipli getirme sözleşmesini tanımlıyorum.
    Task<TaxRate?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada vergi oranlarını sayfalama ve isteğe bağlı aktiflik filtresiyle okuma sözleşmesini tanımlıyorum.
    Task<PagedResult<TaxRate>> GetListAsync(
        int pageNumber,
        int pageSize,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    // Burada vergi oranı adının başka bir kayıtta kullanılıp kullanılmadığını denetleme sözleşmesini tanımlıyorum.
    Task<bool> NameExistsAsync(
        string name,
        Guid? excludedTaxRateId = null,
        CancellationToken cancellationToken = default);

    // Burada ürün ataması veya checkout için vergi oranının aktif olarak bulunup bulunmadığını denetleme sözleşmesini tanımlıyorum.
    Task<bool> ExistsAndActiveAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada toplu ürün oluşturma için yalnız aktif bulunan vergi oranı kimliklerini getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<Guid>> GetActiveExistingIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    // Burada toplu ürün fiyatlarını hesaplamak için etkin vergi oranı kayıtlarını tek sorguda getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyDictionary<Guid, TaxRate>> GetActiveByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);
}
