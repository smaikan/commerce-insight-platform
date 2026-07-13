using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductImageRepository
{
    Task AddAsync(ProductImage image, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductImage>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
}
