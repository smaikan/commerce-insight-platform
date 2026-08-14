using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Search;

namespace ECommerce.Persistence.Repositories;

// Burada storefront katalog sorgularının ortak yayın görünürlüğü kuralını tek yerde tutuyorum.
internal static class PublishedProductQueryExtensions
{
    // Burada yalnız silinmemiş, aktif ve yayın durumundaki ürünleri sorguda bırakıyorum.
    public static IQueryable<Product> WherePublished(this IQueryable<Product> products) =>
        products.Where(product =>
            product.DeletedAtUtc == null &&
            product.IsActive &&
            product.Status == ProductStatus.Active);

    // Burada storefront stok ve fiyat görünürlüğü tercihlerini sayfalama öncesinde SQL sorgusuna uyguluyorum.
    public static IQueryable<Product> ApplyStorefrontVisibility(
        this IQueryable<Product> products,
        bool showOutOfStockProducts,
        bool showProductsWithoutPrice)
    {
        if (!showOutOfStockProducts)
        {
            products = products.Where(product => product.Variants.Any(
                variant => variant.IsActive && variant.Stock > 0));
        }

        if (!showProductsWithoutPrice)
        {
            products = products.Where(product => product.Variants.Any(
                variant => variant.IsActive));
        }

        return products;
    }

    // Burada mağaza görünürlük ayarını aynı SQL komutundaki singleton alt sorgusundan uyguluyorum.
    public static IQueryable<Product> ApplyStorefrontVisibility(
        this IQueryable<Product> products,
        IQueryable<StoreSettings> settings) =>
        products.Where(product =>
            !settings.Any() ||
            ((settings.Any(item => item.ShowOutOfStockProducts) ||
                product.Variants.Any(variant => variant.IsActive && variant.Stock > 0)) &&
             (settings.Any(item => item.ShowProductsWithoutPrice) ||
                product.Variants.Any(variant => variant.IsActive))));
}
