using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Application.Common.Identifiers;
using ECommerce.Application.Tags.Dtos;
using ECommerce.Application.Collections.Dtos;

namespace ECommerce.Application.Products.Dtos;

// Burada ürünün istemciye dönecek ana katalog sözleşmesini tanımlıyorum.
public sealed record ProductDto(
    string Id,
    string Title,
    string MainSku,
    string? Description,
    string Url,
    Guid? TypeId,
    string? TypeName,
    Guid? BrandId,
    string? BrandName,
    Guid? TaxRateId,
    string? TaxRateName,
    decimal? TaxRatePercentage,
    ProductStatus Status,
    bool IsActive,
    bool IsFeatured,
    bool HasVariants,
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
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<TagDto> Tags,
    IReadOnlyList<CollectionDto> Collections,
    IReadOnlyList<ProductImageDto> Images,
    string? Summary,
    ProductImageDto? MainImage);

public static class ProductDtoMapping
{
    // Burada ürün entity'sini dışarıya güvenli public kimlikle dönen DTO'ya çeviriyorum.
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto(
            PublicIdCodec.EncodeProductId(product.Id),
            product.Title,
            product.MainSku,
            product.Description,
            product.Url,
            product.TypeId,
            product.Type?.Name,
            product.BrandId,
            product.Brand?.Name,
            product.TaxRateId,
            product.TaxRate?.Name,
            product.TaxRate?.Rate,
            product.Status,
            product.IsActive,
            product.IsFeatured,
            product.HasVariants,
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
                .ToList(),
            product.ProductTags
                .Where(productTag => productTag.Tag is not null)
                .Select(productTag => productTag.Tag)
                .DistinctBy(tag => tag.Id)
                .OrderBy(tag => tag.Name)
                .Select(tag => tag.ToDto())
                .ToList(),
            product.ProductCollections
                .Where(productCollection => productCollection.Collection is not null)
                .Select(productCollection => productCollection.Collection)
                .DistinctBy(collection => collection.Id)
                .OrderBy(collection => collection.DisplayOrder)
                .ThenBy(collection => collection.Name)
                .Select(collection => collection.ToDto())
                .ToList(),
            product.Images
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => image.ToDto())
                .ToList(),
            product.Description,
            product.Images.ToMainImageDto());
    }
}
