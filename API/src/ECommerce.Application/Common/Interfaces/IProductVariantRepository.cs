using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductVariantRepository
{
    Task AddAsync(ProductVariant variant, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductVariant?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, Guid? excludedVariantId = null, CancellationToken cancellationToken = default);
}
