using ECommerce.Application.Common.Models;
using ECommerce.Application.ProductTypes.Dtos;

namespace ECommerce.Application.Common.Interfaces;

public interface IPublishedProductTypeShowcaseReader
{
    // Burada public kategori vitrin kartlarının sayfalı ve toplu okunmasını tanımlıyorum.
    Task<PagedResult<PublishedProductTypeShowcaseItemDto>> GetListAsync(
        PublishedProductTypeShowcaseFilter filter,
        CancellationToken cancellationToken = default);
}
