using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.Common.Identifiers;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductDto(
    string Id,
    string Title,
    string? Description,
    string Url,
    Guid? TypeId,
    string? TypeName,
    Guid? BrandId,
    string? BrandName,
    ProductStatus Status,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder,
    string? SeoTitle,
    string? SeoDescription,
    long ClickCount,
    long TotalAddToCartCount,
    long TotalPurchaseCount,
    long FavoriteCount,
    long PopularityScore,
    decimal AverageRating,
    long RatingCount,
    long ReviewCount,
    IReadOnlyList<ProductVariantDto> Variants);

public static class ProductDtoMapping
{
    // Burada ürün entity'sini dışarıya güvenli public kimlikle dönen DTO'ya çeviriyorum.
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto(
            PublicIdCodec.EncodeProductId(product.Id),
            product.Title,
            product.Description,
            product.Url,
            product.TypeId,
            product.Type?.Name,
            product.BrandId,
            product.Brand?.Name,
            product.Status,
            product.IsActive,
            product.IsFeatured,
            product.DisplayOrder,
            product.SeoTitle,
            product.SeoDescription,
            product.ClickCount,
            product.TotalAddToCartCount,
            product.TotalPurchaseCount,
            product.FavoriteCount,
            product.PopularityScore,
            product.AverageRating,
            product.RatingCount,
            product.ReviewCount,
            product.Variants
                .OrderBy(variant => variant.Name)
                .ThenBy(variant => variant.Sku)
                .Select(variant => variant.ToDto())
                .ToList());
    }
}
