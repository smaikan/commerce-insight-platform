using ECommerce.API.Security;
using ECommerce.API.Routing;
using ECommerce.Application.Products.Commands.BulkCreateProducts;
using ECommerce.Application.Products.Commands.ChangeProductStatus;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Application.Products.Commands.SetProductActivation;
using ECommerce.Application.Products.Commands.SetProductFeatured;
using ECommerce.Application.Products.Commands.SetProductHasVariants;
using ECommerce.Application.Products.Commands.ReplaceProductPerformanceMetrics;
using ECommerce.Application.Products.Commands.UpdateProduct;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Queries.GetProductById;
using ECommerce.Application.Products.Queries.GetProducts;
using ECommerce.Application.Products.Queries.GetPublishedProducts;
using ECommerce.Application.Products.Queries.GetPublishedProductByUrl;
using ECommerce.Application.Products.Queries.GetProductSeoIndex;
using ECommerce.Application.Products.Relations.Commands.UpdateProductRelations;
using ECommerce.Application.Common.Models;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IOutputCacheStore _outputCacheStore;

    // Burada controller isteklerini Application katmanına iletecek göndericiyi hazırlıyorum.
    public ProductsController(ISender sender, IOutputCacheStore outputCacheStore)
    {
        _sender = sender;
        _outputCacheStore = outputCacheStore;
    }

    // Burada yöneticinin operasyonel ürün listesini sorgu handler'ına iletiyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [DisableRateLimiting]
    [HttpGet]
    [OutputCache(PolicyName = "public-products")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetList([FromQuery] GetProductsQuery query, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada storefront için yalnız yayımlanmış ürün kartlarını anonim olarak getiriyorum.
    [AllowAnonymous]
    [HttpGet("published")]
    [OutputCache(PolicyName = "public-products")]
    public async Task<ActionResult<PagedResult<PublishedProductListItemDto>>> GetPublishedList(
        [FromQuery] GetPublishedProductsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada koleksiyona bağlı yayındaki ürünleri storefront için listeliyorum.
    [AllowAnonymous]
    [HttpGet("by-collection/{collectionId:guid}")]
    [OutputCache(PolicyName = "public-products")]
    public Task<ActionResult<PagedResult<PublishedProductListItemDto>>> GetPublishedByCollection(
        Guid collectionId,
        [FromQuery] PublishedProductListRequest request,
        CancellationToken cancellationToken) =>
        GetPublishedByFilterAsync(request, collectionId: collectionId, cancellationToken: cancellationToken);

    // Burada etikete bağlı yayındaki ürünleri storefront için listeliyorum.
    [AllowAnonymous]
    [HttpGet("by-tag/{tagId:guid}")]
    [OutputCache(PolicyName = "public-products")]
    public Task<ActionResult<PagedResult<PublishedProductListItemDto>>> GetPublishedByTag(
        Guid tagId,
        [FromQuery] PublishedProductListRequest request,
        CancellationToken cancellationToken) =>
        GetPublishedByFilterAsync(request, tagId: tagId, cancellationToken: cancellationToken);

    // Burada türe bağlı yayındaki ürünleri storefront için listeliyorum.
    [AllowAnonymous]
    [HttpGet("by-type/{typeId:guid}")]
    [OutputCache(PolicyName = "public-products")]
    public Task<ActionResult<PagedResult<PublishedProductListItemDto>>> GetPublishedByType(
        Guid typeId,
        [FromQuery] PublishedProductListRequest request,
        CancellationToken cancellationToken) =>
        GetPublishedByFilterAsync(request, typeId: typeId, cancellationToken: cancellationToken);

    // Burada markaya bağlı yayındaki ürünleri storefront için listeliyorum.
    [AllowAnonymous]
    [HttpGet("by-brand/{brandId:guid}")]
    [OutputCache(PolicyName = "public-products")]
    public Task<ActionResult<PagedResult<PublishedProductListItemDto>>> GetPublishedByBrand(
        Guid brandId,
        [FromQuery] PublishedProductListRequest request,
        CancellationToken cancellationToken) =>
        GetPublishedByFilterAsync(request, brandId: brandId, cancellationToken: cancellationToken);

    // Burada ürün URL'siyle storefront detay sorgusunu çalıştırıyorum.
    [AllowAnonymous]
    [HttpGet("by-url/{url}")]
    [OutputCache(PolicyName = "public-products")]
    public async Task<ActionResult<ProductSeoDto>> GetByUrl(
        string url,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetPublishedProductByUrlQuery(url), cancellationToken));

    // Burada arama motorları için yayındaki ürün URL listesini getiriyorum.
    [AllowAnonymous]
    [HttpGet("seo-index")]
    [OutputCache(PolicyName = "public-products")]
    public async Task<ActionResult> GetSeoIndex(
        [FromQuery] GetProductSeoIndexQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada public ürün kimliğiyle anonim ürün detayı getiriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("{id}")]
    [OutputCache(PolicyName = "public-products")]
    public async Task<ActionResult<ProductDto>> GetById(string id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProductByIdQuery(ApiPublicIdParser.ParseProductId(id)), cancellationToken));

    // Burada adminin yeni ürün oluşturma isteğini işliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(command, cancellationToken);
        await EvictProductCacheAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // Burada adminin toplu ürün oluşturma isteğini işliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> BulkCreate(
        BulkCreateProductsCommand command,
        CancellationToken cancellationToken)
    {
        var products = await _sender.Send(command, cancellationToken);
        await EvictProductCacheAsync(cancellationToken);
        return StatusCode(StatusCodes.Status201Created, products);
    }

    // Burada harici katalogdaki ürün performans özetlerini admin yetkisiyle topluca eşitliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("performance-metrics")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> ReplacePerformanceMetrics(
        BulkReplaceProductPerformanceMetricsRequest request,
        CancellationToken cancellationToken)
    {
        var products = await _sender.Send(new ReplaceProductPerformanceMetricsCommand(
            request.Items.Select(item => new ProductPerformanceMetricsItem(
                ApiPublicIdParser.ParseProductId(item.ProductId),
                item.ClickCount,
                item.TotalAddToCartCount,
                item.TotalPurchaseCount,
                item.FavoriteCount,
                item.AverageRating,
                item.RatingCount,
                item.ReviewCount)).ToList()), cancellationToken);
        await EvictProductCacheAsync(cancellationToken);
        return Ok(products);
    }

    // Burada adminin ürün temel bilgilerini, ana SKU değerini ve etiketlerini güncellemesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(
        string id,
        UpdateProductRequest request,
        CancellationToken cancellationToken) =>
        await EvictProductCacheAfterAsync(new UpdateProductCommand(
            ApiPublicIdParser.ParseProductId(id), request.Title, request.MainSku, request.Type, request.Url, request.BrandId, request.Description,
            request.DisplayOrder, request.SeoTitle, request.SeoDescription, request.Tags, request.TaxRateId), cancellationToken);

    // Burada adminin ürün yayın durumunu değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ProductDto>> ChangeStatus(
        string id,
        ChangeProductStatusRequest request,
        CancellationToken cancellationToken) =>
        await EvictProductCacheAfterAsync(new ChangeProductStatusCommand(ApiPublicIdParser.ParseProductId(id), request.Status), cancellationToken);

    // Burada adminin ürünü satışa açıp kapatmasını sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/activation")]
    public async Task<ActionResult<ProductDto>> SetActivation(
        string id,
        SetActivationRequest request,
        CancellationToken cancellationToken) =>
        await EvictProductCacheAfterAsync(new SetProductActivationCommand(ApiPublicIdParser.ParseProductId(id), request.IsActive), cancellationToken);

    // Burada adminin ürünün öne çıkarılma durumunu değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/featured")]
    public async Task<ActionResult<ProductDto>> SetFeatured(
        string id,
        SetFeaturedRequest request,
        CancellationToken cancellationToken) =>
        await EvictProductCacheAfterAsync(new SetProductFeaturedCommand(ApiPublicIdParser.ParseProductId(id), request.IsFeatured), cancellationToken);

    // Burada adminin ürünün varyantlı sunum tercihini değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/has-variants")]
    public Task<ActionResult<ProductDto>> SetHasVariants(
        string id,
        SetHasVariantsRequest request,
        CancellationToken cancellationToken) =>
        EvictProductCacheAfterAsync(new SetProductHasVariantsCommand(
            ApiPublicIdParser.ParseProductId(id), request.HasVariants), cancellationToken);

    // Burada adminin ürün koleksiyon, etiket ve bundle ilişkilerini güncellemesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("{id}/relations")]
    public async Task<IActionResult> UpdateRelations(
        string id,
        UpdateProductRelationsRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new UpdateProductRelationsCommand(
            ApiPublicIdParser.ParseProductId(id),
            request.Collections,
            request.BundleItems.Select(item => new ProductBundleItemInput(
                ApiPublicIdParser.ParseProductId(item.ProductId), item.Quantity)).ToList(),
            request.Tags), cancellationToken);
        await EvictProductCacheAsync(cancellationToken);
        return NoContent();
    }

    // Burada ürün değişikliğinden sonra cache'i temizleyip mevcut başarılı yanıtı koruyorum.
    private async Task<ActionResult<ProductDto>> EvictProductCacheAfterAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : IRequest<ProductDto>
    {
        var product = await _sender.Send(command, cancellationToken);
        await EvictProductCacheAsync(cancellationToken);
        return Ok(product);
    }

    // Burada liste ve detay yanıtlarının güncel kalması için ürün cache etiketini temizliyorum.
    private ValueTask EvictProductCacheAsync(CancellationToken _) =>
        _outputCacheStore.EvictByTagAsync("products", CancellationToken.None);

    // Burada ayrı storefront sınıflandırma rotalarını ortak sorgu sözleşmesine dönüştürüyorum.
    private async Task<ActionResult<PagedResult<PublishedProductListItemDto>>> GetPublishedByFilterAsync(
        PublishedProductListRequest request,
        Guid? typeId = null,
        Guid? brandId = null,
        Guid? collectionId = null,
        Guid? tagId = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetPublishedProductsQuery(
            request.PageNumber,
            request.PageSize,
            typeId,
            brandId,
            collectionId,
            tagId,
            request.SortBy,
            request.Descending), cancellationToken));
}

// Burada ayrı storefront listeleme rotalarının ortak sayfalama ve sıralama alanlarını taşıyorum.
public sealed record PublishedProductListRequest(
    int PageNumber = 1,
    int PageSize = 24,
    PublishedProductSortBy SortBy = PublishedProductSortBy.Newest,
    bool Descending = true);

// Burada ürün temel bilgilerini güncelleyen HTTP isteğini taşıyorum.
public sealed record UpdateProductRequest(
    string Title,
    string MainSku,
    string? Type = null,
    string? Url = null,
    Guid? BrandId = null,
    string? Description = null,
    int DisplayOrder = 0,
    string? SeoTitle = null,
    string? SeoDescription = null,
    IReadOnlyList<string>? Tags = null,
    Guid? TaxRateId = null);

// Burada ürün durum değişikliği isteğini taşıyorum.
public sealed record ChangeProductStatusRequest(ProductStatus Status);

// Burada ürün aktivasyon isteğini taşıyorum.
public sealed record SetActivationRequest(bool IsActive);

// Burada ürün öne çıkarma isteğini taşıyorum.
public sealed record SetFeaturedRequest(bool IsFeatured);

// Burada ürünün varyantlı sunum tercihini taşıyorum.
public sealed record SetHasVariantsRequest(bool HasVariants);

// Burada ürünün tüm ilişki güncelleme isteğini taşıyorum.
public sealed record UpdateProductRelationsRequest(
    IReadOnlyList<string> Collections,
    IReadOnlyList<ProductBundleItemRequest> BundleItems,
    IReadOnlyList<string>? Tags = null);

// Burada toplu performans metriği güncelleme isteğini taşıyorum.
public sealed record BulkReplaceProductPerformanceMetricsRequest(
    IReadOnlyList<ProductPerformanceMetricsRequest> Items);

// Burada tek ürünün dış kaynaklı performans sayaçlarını taşıyorum.
public sealed record ProductPerformanceMetricsRequest(
    string ProductId,
    long ClickCount,
    long TotalAddToCartCount,
    long TotalPurchaseCount,
    long FavoriteCount,
    decimal AverageRating,
    long RatingCount,
    long ReviewCount);

// Burada bundle içine eklenecek ürün ve adet bilgisini taşıyorum.
public sealed record ProductBundleItemRequest(string ProductId, int Quantity);
