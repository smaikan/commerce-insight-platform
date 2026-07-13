using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.UpdateProductVariantPrice;

public sealed record UpdateProductVariantPriceCommand(
    Guid Id,
    decimal Price,
    decimal? CompareAtPrice = null) : IRequest<ProductVariantDto>;
