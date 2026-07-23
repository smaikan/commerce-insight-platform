using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;

public sealed record UpdateProductVariantCommand(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    int Stock,
    decimal? CompareAtPrice = null,
    string? Barcode = null,
    string? Material = null,
    bool IsActive = true,
    string? StockAdjustmentReason = null) : IRequest<ProductVariantDto>;
