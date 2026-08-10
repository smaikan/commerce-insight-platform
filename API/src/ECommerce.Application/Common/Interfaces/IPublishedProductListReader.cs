using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IPublishedProductListReader
{
    // Burada storefront ürün kartlarının filtreli ve sayfalı okunmasını tanımlıyorum.
    Task<PagedResult<PublishedProductListItemDto>> GetListAsync(
        PublishedProductListFilter filter,
        CancellationToken cancellationToken = default);
}
