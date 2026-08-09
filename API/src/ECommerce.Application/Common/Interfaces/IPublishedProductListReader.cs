using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IPublishedProductListReader
{
    Task<PagedResult<PublishedProductListItemDto>> GetListAsync(
        PublishedProductListFilter filter,
        CancellationToken cancellationToken = default);
}
