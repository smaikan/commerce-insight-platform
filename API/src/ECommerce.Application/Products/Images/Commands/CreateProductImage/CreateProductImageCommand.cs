using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Images.Commands.CreateProductImage;

public sealed record CreateProductImageCommand(
    long ProductId,
    string ImageUrl,
    string? AltText = null,
    int DisplayOrder = 0,
    bool IsMain = false) : IRequest<ProductImageDto>;
