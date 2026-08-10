using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductTypeRepository
{
    // Burada yeni ürün türünü kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(ProductType productType, CancellationToken cancellationToken = default);
    // Burada ürün türlerini topluca kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddRangeAsync(IReadOnlyCollection<ProductType> productTypes, CancellationToken cancellationToken = default);
    // Burada ürün türünün varlığını sorgulama sözleşmesini tanımlıyorum.
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada ürün türünü okuma için getirme sözleşmesini tanımlıyorum.
    Task<ProductType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada ürün türünü güncelleme veya silme için takipli getirme sözleşmesini tanımlıyorum.
    Task<ProductType?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada takip edilen ürün türünü kalıcı depodan silme sözleşmesini tanımlıyorum.
    void Remove(ProductType productType);
    // Burada adları eşleşen ürün türlerini getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<ProductType>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    // Burada ürün türü listesini sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<ProductType>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    // Burada mevcut ürün türü kimliklerini topluca getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    // Burada mevcut ürün türü adlarını topluca getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<string>> GetExistingNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    // Burada ürün türü adının başka kayıtta kullanılıp kullanılmadığını sorguluyorum.
    Task<bool> NameExistsAsync(string name, Guid? excludedProductTypeId = null, CancellationToken cancellationToken = default);
}
