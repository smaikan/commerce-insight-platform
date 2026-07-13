using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Images.Queries.GetProductImageById;

public sealed record GetProductImageByIdQuery(Guid Id) : IRequest<ProductImageDto>;
