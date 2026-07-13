using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Images.Queries.GetProductImagesByProductId;

public sealed record GetProductImagesByProductIdQuery(Guid ProductId) : IRequest<IReadOnlyList<ProductImageDto>>;
