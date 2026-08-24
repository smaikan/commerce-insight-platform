using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;

// Burada yönetim kaynaklı imzalı stok hareketi isteğini türü ve gerekçesiyle taşıyorum.
public sealed record UpdateProductVariantStockCommand(
    string ProductVariantSku,
    int QuantityDelta,
    StockMovementType Type,
    string? Reason = null) : IRequest<ProductVariantDto>;
