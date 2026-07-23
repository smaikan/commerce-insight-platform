using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Products.Commands.BulkCreateProducts;

public sealed record BulkCreateProductsCommand(
    IReadOnlyList<BulkCreateProductItem> Products) : IRequest<IReadOnlyList<ProductDto>>;

public sealed record BulkCreateProductItem(
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
    IReadOnlyList<BulkCreateProductVariantItem>? Variants = null,
    IReadOnlyList<BulkCreateProductImageItem>? Images = null,
    IReadOnlyList<Guid>? CollectionIds = null,
    IReadOnlyList<Guid>? TagIds = null);

public sealed record BulkCreateProductVariantItem(
    string Name,
    string Sku,
    decimal Price,
    int Stock,
    decimal? CompareAtPrice = null,
    string? Barcode = null,
    string? Material = null,
    bool IsActive = true);

public sealed record BulkCreateProductImageItem(
    string ImageUrl,
    int DisplayOrder = 0,
    bool IsMain = false,
    string? AltText = null);
