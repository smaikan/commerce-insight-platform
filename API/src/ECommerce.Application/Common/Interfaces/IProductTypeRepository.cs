using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductTypeRepository
{
    Task AddAsync(ProductType productType, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyCollection<ProductType> productTypes, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductType?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductType>> GetByNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductType>> GetListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludedProductTypeId = null, CancellationToken cancellationToken = default);
}
