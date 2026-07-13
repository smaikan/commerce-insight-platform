using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Application.Products.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Title,
    string? Description,
    string Url,
    Guid TypeId,
    string? TypeName,
    Guid? BrandId,
    string? BrandName,
    ProductStatus Status,
    bool IsActive,
    bool IsFeatured,
    int DisplayOrder,
    string? SeoTitle,
    string? SeoDescription,
    int ClickCount,
    int TotalAddToCartCount,
    int TotalPurchaseCount,
    int FavoriteCount,
    decimal AverageRating,
    int RatingCount,
    int ReviewCount);

public static class ProductDtoMapping
{
    public static ProductDto ToDto(this Product product)
    {
        return new ProductDto(
            product.Id,
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
            product.AverageRating,
            product.RatingCount,
            product.ReviewCount);
    }
}
