using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Products.Images.Queries.GetProductImagesByProductId;

public sealed record GetProductImagesByProductIdQuery(
    long ProductId,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductImageDto>>;
