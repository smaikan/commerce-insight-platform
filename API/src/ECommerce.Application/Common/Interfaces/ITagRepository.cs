using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface ITagRepository
{
    // Burada yeni etiketi veritabanı takibine ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);

    // Burada birden fazla etiketi veritabanı takibine ekleme sözleşmesini tanımlıyorum.
    Task AddRangeAsync(IReadOnlyCollection<Tag> tags, CancellationToken cancellationToken = default);

    // Burada etiketi kimliğiyle okuma sözleşmesini tanımlıyorum.
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada etiketi güncelleme amacıyla takipli getirme sözleşmesini tanımlıyorum.
    Task<Tag?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada takip edilen etiketi kalıcı depodan silme sözleşmesini tanımlıyorum.
    void Remove(Tag tag);

    // Burada etiketleri sayfalı getirme sözleşmesini tanımlıyorum.
    Task<PagedResult<Tag>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // Burada verilen kimliklerden mevcut etiket kimliklerini bulma sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    // Burada ad veya URL değerleri eşleşen etiketleri topluca getirme sözleşmesini tanımlıyorum.
    Task<IReadOnlyList<Tag>> GetByNamesOrUrlsAsync(
        IEnumerable<string> names,
        IEnumerable<string> urls,
        CancellationToken cancellationToken = default);

    // Burada verilen adlardan mevcut etiket adlarını bulma sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<string>> GetExistingNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);

    // Burada verilen URL değerlerinden mevcut etiket URL'lerini bulma sözleşmesini tanımlıyorum.
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);

    // Burada etiket adının başka bir kayıtta kullanılıp kullanılmadığını kontrol etme sözleşmesini tanımlıyorum.
    Task<bool> NameExistsAsync(string name, Guid? excludedTagId = null, CancellationToken cancellationToken = default);

    // Burada etiket URL'sinin başka bir kayıtta kullanılıp kullanılmadığını kontrol etme sözleşmesini tanımlıyorum.
    Task<bool> UrlExistsAsync(string url, Guid? excludedTagId = null, CancellationToken cancellationToken = default);
}
