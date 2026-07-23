using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Products.Variants.Queries.GetProductVariantsByProductId;

public sealed record GetProductVariantsByProductIdQuery(
    long ProductId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductVariantDto>>;
