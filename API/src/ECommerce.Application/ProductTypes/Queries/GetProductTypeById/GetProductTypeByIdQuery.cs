using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Queries.GetProductTypeById;

public sealed record GetProductTypeByIdQuery(Guid Id) : IRequest<ProductTypeDto>;
