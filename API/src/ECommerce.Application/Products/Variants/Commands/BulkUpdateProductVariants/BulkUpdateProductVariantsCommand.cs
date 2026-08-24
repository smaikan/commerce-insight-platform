using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.BulkUpdateProductVariants;

public sealed record BulkUpdateProductVariantsCommand(
    long ProductId,
    IReadOnlyList<BulkUpdateProductVariantItem> Variants) : IRequest<IReadOnlyList<ProductVariantDto>>
{
    public const int MaximumBatchSize = 100;
}

public sealed record BulkUpdateProductVariantItem(
    Guid Id,
    string Name,
    string Value,
    string Sku,
    decimal Price,
    int Stock,
    Guid ExpectedConcurrencyToken,
    decimal? CompareAtPrice = null,
    string? Barcode = null,
    string? Material = null,
    bool IsActive = true,
    string? StockAdjustmentReason = null);
