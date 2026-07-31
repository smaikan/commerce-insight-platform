using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductRepository
{
    // Burada yeni ürünü kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    // Burada ürün listesini topluca kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddRangeAsync(IReadOnlyCollection<Product> products, CancellationToken cancellationToken = default);

    // Burada ürünü kimliğiyle detay okumaya yönelik getirme sözleşmesini tanımlıyorum.
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    // Burada ürünleri kimlik listesiyle topluca getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);

    // Burada vergi oranı değiştiğinde bağlı ürünleri varyantlarıyla takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<Product>> GetByTaxRateIdForUpdateAsync(Guid taxRateId, CancellationToken cancellationToken = default);

    // Burada ürünü güncelleme için takipli getirme sözleşmesini tanımlıyorum.
    Task<Product?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);

    // Burada checkout gibi toplu güncelleme işlemleri için ürünleri kararlı sırayla takipli getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<Product>> GetByIdsForUpdateAsync(
        IEnumerable<long> ids,
        CancellationToken cancellationToken = default);

    // Burada ürünü ilişkileriyle güncelleme için getirme sözleşmesini tanımlıyorum.
    Task<Product?> GetWithRelationsForUpdateAsync(long id, CancellationToken cancellationToken = default);

    // Burada filtrelenmiş ürün sayfasını getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<Product>> GetListAsync(ProductListFilter filter, CancellationToken cancellationToken = default);

    // Burada URL değerinin başka üründe kullanılıp kullanılmadığını kontrol ediyorum.
    Task<bool> UrlExistsAsync(string url, long? excludedProductId = null, CancellationToken cancellationToken = default);

    // Burada ana SKU değerinin başka üründe kullanılıp kullanılmadığını kontrol ediyorum.
    Task<bool> MainSkuExistsAsync(
        string mainSku,
        long? excludedProductId = null,
        CancellationToken cancellationToken = default);

    // Burada URL listesindeki mevcut değerleri getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);

    // Burada ana SKU listesindeki mevcut değerleri getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<string>> GetExistingMainSkusAsync(
        IEnumerable<string> mainSkus,
        CancellationToken cancellationToken = default);

    // Burada varyant SKU listesindeki mevcut değerleri getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<string>> GetExistingVariantSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default);
}
