using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetProductSeoIndex;

public sealed record GetProductSeoIndexQuery(
    int PageNumber = 1,
    int PageSize = 100) : IRequest<PagedResult<ProductSeoIndexItemDto>>;
