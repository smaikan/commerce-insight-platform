using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Models;

namespace ECommerce.Application.Common.Interfaces;

public interface IPublishedCollectionShowcaseReader
{
    // Burada public koleksiyon vitrin kartlarının sayfalı ve toplu okunmasını tanımlıyorum.
    Task<PagedResult<PublishedCollectionShowcaseItemDto>> GetListAsync(
        PublishedCollectionShowcaseFilter filter,
        CancellationToken cancellationToken = default);
}
