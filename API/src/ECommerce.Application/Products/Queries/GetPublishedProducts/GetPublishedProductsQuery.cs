using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetPublishedProducts;

// Burada storefront için yalnız yayındaki ürünleri sayfalı getirme isteğini tanımlıyorum.
public sealed record GetPublishedProductsQuery(
    int PageNumber = 1,
    int PageSize = 24,
    PublishedProductSortBy SortBy = PublishedProductSortBy.Newest,
    bool Descending = true) : IRequest<PagedResult<PublishedProductListItemDto>>;
