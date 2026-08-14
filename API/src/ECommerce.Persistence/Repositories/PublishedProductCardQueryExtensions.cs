using ECommerce.Domain.Entities;

namespace ECommerce.Persistence.Repositories;

// Burada public liste ve öneri endpointlerinin fiyat, görsel ve stok projeksiyonunu ortak tutuyorum.
internal static class PublishedProductCardQueryExtensions
{
    // Burada yalnız DTO'ların ihtiyaç duyduğu kart kolonlarını SQL seviyesinde projekte ediyorum.
    public static IQueryable<PublishedProductCardProjection> SelectPublishedProductCards(
        this IQueryable<Product> products,
        bool showStockWarning = false,
        int lowStockThreshold = 5) =>
        products.Select(product => new PublishedProductCardProjection(
            product.Id,
            product.Title,
            product.Url,
            product.Description,
            product.Brand == null ? null : product.Brand.Name,
            product.AverageRating,
            product.RatingCount,
            product.Variants
                .Where(variant => variant.IsActive)
                .OrderBy(variant => variant.Price)
                .ThenBy(variant => variant.Id)
                .Select(variant => new ProductPriceProjection(variant.Price, variant.CompareAtPrice))
                .FirstOrDefault(),
            product.Images
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => new ProductImageProjection(
                    image.Id,
                    image.ImageUrl,
                    image.AltText,
                    image.DisplayOrder,
                    image.IsMain))
                .FirstOrDefault(),
            product.Variants.Any(variant => variant.IsActive && variant.Stock > 0),
            product.Variants
                .Where(variant => variant.IsActive && variant.Stock > 0)
                .Select(variant => (int?)variant.Stock)
                .Min(),
            showStockWarning && product.Variants.Any(
                variant => variant.IsActive && variant.Stock > 0 && variant.Stock <= lowStockThreshold)));

    // Burada düşük stok tercihini aynı SQL komutundaki singleton StoreSettings alt sorgusundan uyguluyorum.
    public static IQueryable<PublishedProductCardProjection> SelectPublishedProductCards(
        this IQueryable<Product> products,
        IQueryable<StoreSettings> settings) =>
        products.Select(product => new PublishedProductCardProjection(
            product.Id,
            product.Title,
            product.Url,
            product.Description,
            product.Brand == null ? null : product.Brand.Name,
            product.AverageRating,
            product.RatingCount,
            product.Variants
                .Where(variant => variant.IsActive)
                .OrderBy(variant => variant.Price)
                .ThenBy(variant => variant.Id)
                .Select(variant => new ProductPriceProjection(variant.Price, variant.CompareAtPrice))
                .FirstOrDefault(),
            product.Images
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => new ProductImageProjection(
                    image.Id,
                    image.ImageUrl,
                    image.AltText,
                    image.DisplayOrder,
                    image.IsMain))
                .FirstOrDefault(),
            product.Variants.Any(variant => variant.IsActive && variant.Stock > 0),
            product.Variants
                .Where(variant => variant.IsActive && variant.Stock > 0)
                .Select(variant => (int?)variant.Stock)
                .Min(),
            settings.Any(item => item.ShowStockWarning) && product.Variants.Any(variant =>
                variant.IsActive &&
                variant.Stock > 0 &&
                variant.Stock <= settings.Select(item => item.LowStockThreshold).FirstOrDefault())));

    // Burada suggestion DTO'sunun ihtiyaç duyduğu en küçük fiyat, görsel ve stok projeksiyonunu aynı semantikle üretiyorum.
    public static IQueryable<PublishedProductSuggestionCardProjection> SelectPublishedProductSuggestionCards(
        this IQueryable<Product> products) =>
        products.Select(product => new PublishedProductSuggestionCardProjection(
            product.Id,
            product.Title,
            product.Url,
            product.Brand == null ? null : product.Brand.Name,
            product.Variants
                .Where(variant => variant.IsActive)
                .OrderBy(variant => variant.Price)
                .ThenBy(variant => variant.Id)
                .Select(variant => (decimal?)variant.Price)
                .FirstOrDefault(),
            product.Variants
                .Where(variant => variant.IsActive)
                .OrderBy(variant => variant.Price)
                .ThenBy(variant => variant.Id)
                .Select(variant => variant.CompareAtPrice)
                .FirstOrDefault(),
            product.Images
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => image.ImageUrl)
                .FirstOrDefault(),
            product.Images
                .OrderByDescending(image => image.IsMain)
                .ThenBy(image => image.DisplayOrder)
                .ThenBy(image => image.Id)
                .Select(image => image.AltText)
                .FirstOrDefault(),
            product.Variants.Any(variant => variant.IsActive && variant.Stock > 0)));
}

// Burada ortak public ürün kartı SQL projeksiyonunu taşıyorum.
internal sealed record PublishedProductCardProjection(
    long Id,
    string Title,
    string Url,
    string? Summary,
    string? BrandName,
    decimal AverageRating,
    long RatingCount,
    ProductPriceProjection? Price,
    ProductImageProjection? MainImage,
    bool IsAvailable,
    int? LowestAvailableStock,
    bool IsLowStock);

// Burada ortak kart fiyat özetini taşıyorum.
internal sealed record ProductPriceProjection(decimal Price, decimal? CompareAtPrice);

// Burada ortak kart ana görsel özetini taşıyorum.
internal sealed record ProductImageProjection(
    Guid Id,
    string ImageUrl,
    string? AltText,
    int DisplayOrder,
    bool IsMain);

// Burada suggestion cevabına gereken en küçük ortak kart projeksiyonunu taşıyorum.
internal sealed record PublishedProductSuggestionCardProjection(
    long Id,
    string Title,
    string Url,
    string? BrandName,
    decimal? Price,
    decimal? CompareAtPrice,
    string? ImageUrl,
    string? ImageAlt,
    bool IsAvailable);
