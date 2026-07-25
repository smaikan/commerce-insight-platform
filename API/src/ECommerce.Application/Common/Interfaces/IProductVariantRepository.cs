using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductVariantRepository
{
    // Burada yeni ürün varyantını kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    // Burada varyantı silinmek üzere işaretleme sözleşmesini tanımlıyorum.
    void Remove(ProductVariant variant);
    // Burada varyantı okuma amaçlı takip etmeden getirme sözleşmesini tanımlıyorum.
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada varyantı güncelleme için takipli getirme sözleşmesini tanımlıyorum.
    Task<ProductVariant?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada checkout gibi toplu güncelleme işlemleri için varyantları kararlı sırayla takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<ProductVariant>> GetByIdsForUpdateAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);
    // Burada bir ürüne ait varyantları sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<ProductVariant>> GetByProductIdAsync(
        long productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    // Burada bir ürüne ait varyant sayısını getirme sözleşmesini tanımlıyorum.
    Task<int> CountByProductIdAsync(long productId, CancellationToken cancellationToken = default);
    // Burada varyantın silinemez denetim geçmişi taşıyıp taşımadığını kontrol etme sözleşmesini tanımlıyorum.
    Task<bool> HasStockMovementsAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada SKU bilgisinin başka bir varyantta kullanılıp kullanılmadığını kontrol etme sözleşmesini tanımlıyorum.
    Task<bool> SkuExistsAsync(string sku, Guid? excludedVariantId = null, CancellationToken cancellationToken = default);
}
