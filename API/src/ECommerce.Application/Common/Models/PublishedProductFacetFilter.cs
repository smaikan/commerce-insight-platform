namespace ECommerce.Application.Common.Models;

// Burada yayımlanmış katalog facetlerinin ortak sınıflandırma filtrelerini taşıyorum.
public sealed record PublishedProductFacetFilter(
    Guid? TypeId = null,
    Guid? BrandId = null,
    Guid? CollectionId = null,
    Guid? TagId = null,
    bool ShowOutOfStockProducts = true,
    bool ShowProductsWithoutPrice = true);

// Burada ayrı public endpointlerin hesaplayacağı facet boyutlarını tanımlıyorum.
public enum PublishedProductFacetDimension
{
    Brand,
    Collection,
    ProductType
}
