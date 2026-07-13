using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface ICollectionRepository
{
    Task AddAsync(Collection collection, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyCollection<Collection> collections, CancellationToken cancellationToken = default);
    Task<Collection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Collection?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Collection>> GetListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
    Task<bool> UrlExistsAsync(string url, Guid? excludedCollectionId = null, CancellationToken cancellationToken = default);
}
