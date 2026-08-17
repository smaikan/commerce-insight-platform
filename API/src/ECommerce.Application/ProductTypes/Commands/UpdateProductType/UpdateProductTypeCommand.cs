using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.UpdateProductType;

public sealed record UpdateProductTypeCommand(
    Guid Id,
    string Name,
    string? Description = null,
    string? ImageUrl = null) : IRequest<ProductTypeDto>;
