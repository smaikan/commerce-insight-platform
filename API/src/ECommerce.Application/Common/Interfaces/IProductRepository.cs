using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyCollection<Product> products, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<Product?> GetWithRelationsForUpdateAsync(long id, CancellationToken cancellationToken = default);
    Task<PagedResult<Product>> GetListAsync(ProductListFilter filter, CancellationToken cancellationToken = default);
    Task<bool> UrlExistsAsync(string url, long? excludedProductId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingVariantSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default);
}
