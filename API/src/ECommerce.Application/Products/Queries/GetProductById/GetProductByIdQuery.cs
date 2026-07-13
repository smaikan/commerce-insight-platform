using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
