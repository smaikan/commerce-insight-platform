using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface ICollectionRepository
{
    // Burada yeni koleksiyonu kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(Collection collection, CancellationToken cancellationToken = default);
    // Burada koleksiyonları topluca kalıcı depoya ekleme sözleşmesini tanımlıyorum.
    Task AddRangeAsync(IReadOnlyCollection<Collection> collections, CancellationToken cancellationToken = default);
    // Burada koleksiyonu okuma için getirme sözleşmesini tanımlıyorum.
    Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada koleksiyonu güncelleme veya silme için takipli getirme sözleşmesini tanımlıyorum.
    Task<Collection?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    // Burada takip edilen koleksiyonu kalıcı depodan silme sözleşmesini tanımlıyorum.
    void Remove(Collection collection);
    // Burada ad veya URL değerleriyle eşleşen koleksiyonları getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<Collection>> GetByNamesOrUrlsAsync(IEnumerable<string> names, IEnumerable<string> urls, CancellationToken cancellationToken = default);
    // Burada koleksiyon listesini sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<Collection>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    // Burada mevcut koleksiyon kimliklerini topluca getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    // Burada mevcut koleksiyon URL değerlerini topluca getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
    // Burada koleksiyon URL değerinin başka kayıtta kullanılıp kullanılmadığını sorguluyorum.
    Task<bool> UrlExistsAsync(string url, Guid? excludedCollectionId = null, CancellationToken cancellationToken = default);
}
