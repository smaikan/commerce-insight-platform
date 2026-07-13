using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Images.Commands.UpdateProductImage;

public sealed record UpdateProductImageCommand(
    Guid Id,
    string ImageUrl,
    string? AltText = null,
    int DisplayOrder = 0,
    bool IsMain = false) : IRequest<ProductImageDto>;
