using ECommerce.Domain.Entities;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IProductImageRepository
{
    Task AddAsync(ProductImage image, CancellationToken cancellationToken = default);
    void Remove(ProductImage image);
    Task<ProductImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetMainByProductIdForUpdateAsync(
        long productId,
        Guid? excludedImageId = null,
        CancellationToken cancellationToken = default);
    Task<int> CountByProductIdAsync(long productId, CancellationToken cancellationToken = default);
    Task<ProductImage?> GetFirstByProductIdForUpdateAsync(
        long productId,
        Guid excludedImageId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<ProductImage>> GetByProductIdAsync(
        long productId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
