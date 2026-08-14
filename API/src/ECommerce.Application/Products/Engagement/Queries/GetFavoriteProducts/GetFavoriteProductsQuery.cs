using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetFavoriteProducts;

// Burada favori sayfalamasını ve anonim sahiplik için isteğe bağlı session değerini taşıyorum.
public sealed record GetFavoriteProductsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SessionId = null) : IRequest<PagedResult<ProductDto>>;
