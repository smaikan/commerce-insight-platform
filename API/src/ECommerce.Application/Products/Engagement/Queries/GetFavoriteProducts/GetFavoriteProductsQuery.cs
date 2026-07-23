using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Engagement.Queries.GetFavoriteProducts;

public sealed record GetFavoriteProductsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
