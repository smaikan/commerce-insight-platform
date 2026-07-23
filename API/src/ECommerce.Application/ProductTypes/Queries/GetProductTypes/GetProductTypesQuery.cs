using ECommerce.Application.ProductTypes.Dtos;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.ProductTypes.Queries.GetProductTypes;

public sealed record GetProductTypesQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PagedResult<ProductTypeDto>>;
