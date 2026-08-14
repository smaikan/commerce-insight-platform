using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetPublishedProductFacets;

// Burada ayrı facet endpointlerinden gelen ortak yayımlanmış katalog sorgusunu tanımlıyorum.
public sealed record GetPublishedProductFacetsQuery(
    PublishedProductFacetDimension Dimension,
    Guid? TypeId = null,
    Guid? BrandId = null,
    Guid? CollectionId = null,
    Guid? TagId = null) : IRequest<IReadOnlyList<PublishedProductFacetItemDto>>;
