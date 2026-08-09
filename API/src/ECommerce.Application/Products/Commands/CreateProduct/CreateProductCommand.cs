using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace ECommerce.Application.Products.Commands.CreateProduct;

// Burada tek ürün oluşturma isteğinin tüm alanlarını taşıyorum.
public sealed record CreateProductCommand(
    string Title,
    string MainSku,
    bool HasVariants = false,
    string? Type = null,
    string? Url = null,
    Guid? BrandId = null,
    string? Description = null,
    ProductStatus Status = ProductStatus.Draft,
    bool IsActive = true,
    bool IsFeatured = false,
    int DisplayOrder = 0,
    string? SeoTitle = null,
    string? SeoDescription = null,
    IReadOnlyList<string>? Collections = null,
    IReadOnlyList<CreateProductVariantItem>? Variants = null,
    IReadOnlyList<string>? Tags = null,
    Guid? TaxRateId = null) : IRequest<ProductDto>;

// Burada ürünle birlikte oluşturulacak varyant bilgisini taşıyorum.
[method: JsonConstructor]
public sealed record CreateProductVariantItem(
    string Name,
    string Value,
    string Sku,
    decimal Price,
    int Stock,
    decimal? CompareAtPrice = null,
    string? Barcode = null,
    string? Material = null,
    bool IsActive = true,
    decimal? OpeningUnitCostExcludingVat = null,
    decimal? OpeningUnitCostIncludingVat = null)
{
    // Burada eski uygulama çağrılarını tek seçenek değerine eşleyerek uyumlu tutuyorum.
    public CreateProductVariantItem(
        string Name,
        string Sku,
        decimal Price,
        int Stock,
        decimal? CompareAtPrice = null,
        string? Barcode = null,
        string? Material = null,
        bool IsActive = true,
        decimal? OpeningUnitCostExcludingVat = null,
        decimal? OpeningUnitCostIncludingVat = null)
        : this(Name, Name, Sku, Price, Stock, CompareAtPrice, Barcode, Material, IsActive, OpeningUnitCostExcludingVat, OpeningUnitCostIncludingVat)
    {
    }
}
