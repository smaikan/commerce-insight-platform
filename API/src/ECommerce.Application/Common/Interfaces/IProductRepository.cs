using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyCollection<Product> products, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetListAsync(CancellationToken cancellationToken = default);
    Task<bool> UrlExistsAsync(string url, Guid? excludedProductId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingVariantSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default);
}
