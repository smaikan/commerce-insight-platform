using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Commands.ChangeProductStatus;

public sealed record ChangeProductStatusCommand(Guid Id, ProductStatus Status) : IRequest<ProductDto>;
