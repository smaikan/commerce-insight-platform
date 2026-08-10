using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IBrandRepository
{
    // Burada yeni markayı kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(Brand brand, CancellationToken cancellationToken = default);
    // Burada markaları topluca kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddRangeAsync(IReadOnlyCollection<Brand> brands, CancellationToken cancellationToken = default);
    // Burada marka kaydının varlığını sorgulama sözleşmesini tanımlıyorum.
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada markayı okuma için getirme sözleşmesini tanımlıyorum.
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada markayı güncelleme veya silme için takipli getirme sözleşmesini tanımlıyorum.
    Task<Brand?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada takip edilen markayı kalıcı depodan silme sözleşmesini tanımlıyorum.
    void Remove(Brand brand);
    // Burada marka listesini sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<Brand>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    // Burada mevcut marka kimliklerini topluca getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    // Burada mevcut marka URL değerlerini topluca getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
    // Burada marka URL değerinin başka kayıtta kullanılıp kullanılmadığını sorguluyorum.
    Task<bool> UrlExistsAsync(string url, Guid? excludedBrandId = null, CancellationToken cancellationToken = default);
}
