using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;

public sealed record UpdateProductVariantStockCommand(
    Guid Id,
    int Quantity,
    string? Reason = null) : IRequest<ProductVariantDto>;
