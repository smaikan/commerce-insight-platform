using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.UpdateProduct;

// Burada ürünün temel bilgilerini değiştirecek isteği taşıyorum.
public sealed record UpdateProductCommand(
    long Id,
    string Title,
    string MainSku,
    Guid? TypeId,
    string? Url = null,
    Guid? BrandId = null,
    string? Description = null,
    int DisplayOrder = 0,
    string? SeoTitle = null,
    string? SeoDescription = null,
    IReadOnlyList<string>? Tags = null,
    Guid? TaxRateId = null) : IRequest<ProductDto>;
