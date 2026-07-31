using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;

public sealed record UpdateProductVariantCommand(
    Guid Id,
    string Name,
    string Value,
    string Sku,
    decimal Price,
    int Stock,
    decimal? CompareAtPrice = null,
    string? Barcode = null,
    string? Material = null,
    bool IsActive = true,
    string? StockAdjustmentReason = null) : IRequest<ProductVariantDto>
{
    // Burada eski tek metinli varyant güncellemelerini aynı adı değer olarak kullanarak uyumlu tutuyorum.
    public UpdateProductVariantCommand(
        Guid Id,
        string Name,
        string Sku,
        decimal Price,
        int Stock,
        decimal? CompareAtPrice = null,
        string? Barcode = null,
        string? Material = null,
        bool IsActive = true,
        string? StockAdjustmentReason = null)
        : this(Id, Name, Name, Sku, Price, Stock, CompareAtPrice, Barcode, Material, IsActive, StockAdjustmentReason)
    {
    }
}
