using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.CreateProductVariant;

// Burada tek varyant oluşturma isteğini opsiyonel açılış maliyetleriyle taşıyorum.
public sealed record CreateProductVariantCommand(
    long ProductId,
    string Name,
    string Sku,
    decimal Price,
    int Stock,
    decimal? CompareAtPrice = null,
    string? Barcode = null,
    string? Material = null,
    bool IsActive = true,
    decimal? OpeningUnitCostExcludingVat = null,
    decimal? OpeningUnitCostIncludingVat = null) : IRequest<ProductVariantDto>;
