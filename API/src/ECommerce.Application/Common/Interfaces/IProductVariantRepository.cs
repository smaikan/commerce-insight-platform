using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductVariantRepository
{
    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    void Remove(ProductVariant variant);
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductVariant>> GetByProductIdAsync(
        long productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<int> CountByProductIdAsync(long productId, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludedVariantId = null, CancellationToken cancellationToken = default);
}
