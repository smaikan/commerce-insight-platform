using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.CreateProductVariant;

public sealed record CreateProductVariantCommand(
    Guid ProductId,
    string Sku,
    decimal Price,
    int Stock,
    decimal? CompareAtPrice = null,
    string? Barcode = null,
    string? Color = null,
    string? Size = null,
    string? Material = null,
    bool IsActive = true) : IRequest<ProductVariantDto>;
