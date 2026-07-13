using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Queries.GetProductTypes;

public sealed record GetProductTypesQuery : IRequest<IReadOnlyList<ProductTypeDto>>;
