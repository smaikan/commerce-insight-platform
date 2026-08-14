using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;

namespace ECommerce.Application.Common.Interfaces;

// Burada yayımlanmış katalog facetlerini persistence ayrıntılarından bağımsız okuyorum.
public interface IPublishedProductFacetReader
{
    // Burada istenen facet boyutunu diğer seçili boyutlara göre adetli getiriyorum.
    Task<IReadOnlyList<PublishedProductFacetItemDto>> GetFacetsAsync(
        PublishedProductFacetDimension dimension,
        PublishedProductFacetFilter filter,
        CancellationToken cancellationToken = default);
}
