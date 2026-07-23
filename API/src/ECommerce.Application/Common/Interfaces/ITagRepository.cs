using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface ITagRepository
{
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyCollection<Tag> tags, CancellationToken cancellationToken = default);
    Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tag?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Tag>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludedTagId = null, CancellationToken cancellationToken = default);
    Task<bool> UrlExistsAsync(string url, Guid? excludedTagId = null, CancellationToken cancellationToken = default);
}
