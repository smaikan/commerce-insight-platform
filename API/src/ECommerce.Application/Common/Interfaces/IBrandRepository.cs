using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IBrandRepository
{
    Task AddAsync(Brand brand, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyCollection<Brand> brands, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Brand?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Brand>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
    Task<bool> UrlExistsAsync(string url, Guid? excludedBrandId = null, CancellationToken cancellationToken = default);
}
