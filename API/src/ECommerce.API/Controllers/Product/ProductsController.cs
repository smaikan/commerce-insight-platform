using ECommerce.API.Security;
using ECommerce.API.Routing;
using ECommerce.Application.Products.Commands.BulkCreateProducts;
using ECommerce.Application.Products.Commands.ChangeProductStatus;
using ECommerce.Application.Products.Commands.CreateProduct;
using ECommerce.Application.Products.Commands.SetProductActivation;
using ECommerce.Application.Products.Commands.SetProductFeatured;
using ECommerce.Application.Products.Commands.UpdateProduct;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Queries.GetProductById;
using ECommerce.Application.Products.Queries.GetProducts;
using ECommerce.Application.Products.Relations.Commands.UpdateProductRelations;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada controller isteklerini Application katmanına iletecek göndericiyi hazırlıyorum.
    public ProductsController(ISender sender) => _sender = sender;

    // Burada anonim ürün listeleme isteğini sorgu handler'ına iletiyorum.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetProductsQuery query, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada public ürün kimliğiyle anonim ürün detayı getiriyorum.
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetById(string id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProductByIdQuery(ApiPublicIdParser.ParseProductId(id)), cancellationToken));

    // Burada adminin yeni ürün oluşturma isteğini işliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // Burada adminin toplu ürün oluşturma isteğini işliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> BulkCreate(
        BulkCreateProductsCommand command,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _sender.Send(command, cancellationToken));

    // Burada adminin ürün temel bilgilerini, ana SKU değerini ve etiketlerini güncellemesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ProductDto>> Update(
        string id,
        UpdateProductRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new UpdateProductCommand(
            ApiPublicIdParser.ParseProductId(id), request.Title, request.MainSku, request.TypeId, request.Url, request.BrandId, request.Description,
            request.DisplayOrder, request.SeoTitle, request.SeoDescription, request.Tags, request.TaxRateId), cancellationToken));

    // Burada adminin ürün yayın durumunu değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ProductDto>> ChangeStatus(
        string id,
        ChangeProductStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ChangeProductStatusCommand(ApiPublicIdParser.ParseProductId(id), request.Status), cancellationToken));

    // Burada adminin ürünü satışa açıp kapatmasını sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/activation")]
    public async Task<ActionResult<ProductDto>> SetActivation(
        string id,
        SetActivationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new SetProductActivationCommand(ApiPublicIdParser.ParseProductId(id), request.IsActive), cancellationToken));

    // Burada adminin ürünün öne çıkarılma durumunu değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id}/featured")]
    public async Task<ActionResult<ProductDto>> SetFeatured(
        string id,
        SetFeaturedRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new SetProductFeaturedCommand(ApiPublicIdParser.ParseProductId(id), request.IsFeatured), cancellationToken));

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
            request.CollectionIds,
            request.TagIds ?? [],
            request.BundleItems.Select(item => new ProductBundleItemInput(
                ApiPublicIdParser.ParseProductId(item.ProductId), item.Quantity)).ToList(),
            request.Tags), cancellationToken);
        return NoContent();
    }
}

// Burada ürün temel bilgilerini güncelleyen HTTP isteğini taşıyorum.
public sealed record UpdateProductRequest(
    string Title,
    string MainSku,
    Guid? TypeId = null,
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

// Burada ürünün tüm ilişki güncelleme isteğini taşıyorum.
public sealed record UpdateProductRelationsRequest(
    IReadOnlyList<Guid> CollectionIds,
    IReadOnlyList<Guid>? TagIds,
    IReadOnlyList<ProductBundleItemRequest> BundleItems,
    IReadOnlyList<string>? Tags = null);

// Burada bundle içine eklenecek ürün ve adet bilgisini taşıyorum.
public sealed record ProductBundleItemRequest(string ProductId, int Quantity);
