using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetPublishedProducts;

// Burada storefront için yalnız yayındaki ürünleri sayfalı getirme isteğini tanımlıyorum.
public sealed record GetPublishedProductsQuery(
    int PageNumber = 1,
    int PageSize = 24,
    Guid? TypeId = null,
    Guid? BrandId = null,
    Guid? CollectionId = null,
    Guid? TagId = null,
    PublishedProductSortBy? SortBy = null,
    bool? Descending = null,
    string? Search = null) : IRequest<PagedResult<PublishedProductListItemDto>>;
