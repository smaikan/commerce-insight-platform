using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.CreateProductType;

public sealed record CreateProductTypeCommand(
    string Name,
    string? Description = null,
    bool IsActive = true,
    string? ImageUrl = null) : IRequest<ProductTypeDto>;
