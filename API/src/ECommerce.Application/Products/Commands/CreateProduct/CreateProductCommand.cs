using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Title,
    Guid? TypeId = null,
    string? Url = null,
    Guid? BrandId = null,
    string? Description = null,
    ProductStatus Status = ProductStatus.Draft,
    bool IsActive = true,
    bool IsFeatured = false,
    int DisplayOrder = 0,
    string? SeoTitle = null,
    string? SeoDescription = null,
    IReadOnlyList<Guid>? CollectionIds = null,
    IReadOnlyList<CreateProductVariantItem>? Variants = null) : IRequest<ProductDto>;

public sealed record CreateProductVariantItem(
    string Name,
    string Sku,
    decimal Price,
    int Stock,
    decimal? CompareAtPrice = null,
    string? Barcode = null,
    string? Material = null,
    bool IsActive = true);
