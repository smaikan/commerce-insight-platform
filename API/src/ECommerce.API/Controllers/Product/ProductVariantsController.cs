using ECommerce.API.Security;
using ECommerce.API.Routing;
using ECommerce.API.ErrorHandling;
using ECommerce.API.OutputCaching;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Variants.Commands.CreateProductVariant;
using ECommerce.Application.Products.Variants.Commands.BulkUpdateProductVariants;
using ECommerce.Application.Products.Variants.Commands.DeleteProductVariant;
using ECommerce.Application.Products.Variants.Commands.SetProductVariantActivation;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariant;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariantPrice;
using ECommerce.Application.Products.Variants.Commands.UpdateProductVariantStock;
using ECommerce.Application.Products.Variants.Queries.GetProductVariantById;
using ECommerce.Application.Products.Variants.Queries.GetProductVariantsByProductId;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/product-variants")]
[ServiceFilter(typeof(ProductOutputCacheInvalidationFilter))]
public sealed class ProductVariantsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada varyant HTTP uçlarını Application katmanına iletecek MediatR bağımlılığını hazırlıyorum.
    public ProductVariantsController(ISender sender) => _sender = sender;

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    // Burada tek varyantın herkese açık detayını getiriyorum.
    public async Task<ActionResult<ProductVariantDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProductVariantByIdQuery(id), cancellationToken));

    [AllowAnonymous]
    [HttpGet("by-product/{productId}")]
    // Burada bir ürüne bağlı varyantları sayfalı ve herkese açık getiriyorum.
    public async Task<ActionResult> GetByProduct(
        string productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetProductVariantsByProductIdQuery(
            ApiPublicIdParser.ParseProductId(productId), pageNumber, pageSize), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("by-product/{productId}")]
    // Burada yönetici için açılış stok hareketiyle birlikte yeni varyant oluşturuyorum.
    public async Task<ActionResult<ProductVariantDto>> Create(
        string productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _sender.Send(new CreateProductVariantCommand(
            ApiPublicIdParser.ParseProductId(productId), request.Name, request.Value, request.Sku, request.Price, request.Stock, request.CompareAtPrice,
            request.Barcode, request.Material, request.IsActive), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("{id:guid}")]
    // Burada varyant detaylarını ve gerekirse stok sayım farkını tek komuta iletiyorum.
    public async Task<ActionResult<ProductVariantDto>> Update(
        Guid id,
        UpdateProductVariantRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new UpdateProductVariantCommand(
            id, request.Name, request.Value, request.Sku, request.Price, request.Stock, request.CompareAtPrice,
            request.Barcode, request.Material, request.IsActive, request.StockAdjustmentReason), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("by-product/{productId}/bulk")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductVariantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProductVariantBulkProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProductVariantBulkProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    // Burada aynı ürüne ait varyantları tek atomik batch komutuna iletiyorum.
    public async Task<ActionResult<IReadOnlyList<ProductVariantDto>>> BulkUpdate(
        string productId,
        BulkUpdateProductVariantsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new BulkUpdateProductVariantsCommand(
            ApiPublicIdParser.ParseProductId(productId),
            request.Variants.Select(item => new BulkUpdateProductVariantItem(
                item.Id,
                item.Name,
                item.Value,
                item.Sku,
                item.Price,
                item.Stock,
                item.ExpectedConcurrencyToken,
                item.CompareAtPrice,
                item.Barcode,
                item.Material,
                item.IsActive,
                item.StockAdjustmentReason)).ToList()), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id:guid}/price")]
    // Burada yalnız varyant fiyatlarını yönetici yetkisiyle güncelliyorum.
    public async Task<ActionResult<ProductVariantDto>> UpdatePrice(
        Guid id,
        UpdateVariantPriceRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new UpdateProductVariantPriceCommand(id, request.Price, request.CompareAtPrice), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("stock-movements")]
    [ProducesResponseType(typeof(ProductVariantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    // Burada yönetici kaynaklı imzalı stok hareketini varyant SKU'su, türü ve gerekçesiyle kaydediyorum.
    public async Task<ActionResult<ProductVariantDto>> AdjustStock(
        AdjustStockRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new UpdateProductVariantStockCommand(
            request.ProductVariantSku,
            request.QuantityDelta,
            request.Type,
            request.Reason), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id:guid}/activation")]
    // Burada varyantın satış aktivasyonunu yönetici yetkisiyle değiştiriyorum.
    public async Task<ActionResult<ProductVariantDto>> SetActivation(
        Guid id,
        SetActivationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new SetProductVariantActivationCommand(id, request.IsActive), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    // Burada son varyant korumasını Application katmanında işleterek varyantı siliyorum.
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductVariantCommand(id), cancellationToken);
        return NoContent();
    }
}

// Burada yeni varyantın başlangıç bilgileri ve açılış stok bakiyesini taşıyorum.
public sealed record CreateProductVariantRequest(
    string Name, string Value, string Sku, decimal Price, int Stock, decimal? CompareAtPrice = null,
    string? Barcode = null, string? Material = null, bool IsActive = true);
// Burada varyant detaylarıyla olası stok sayım hedefini birlikte taşıyorum.
public sealed record UpdateProductVariantRequest(
    string Name, string Value, string Sku, decimal Price, int Stock, decimal? CompareAtPrice = null,
    string? Barcode = null, string? Material = null, bool IsActive = true,
    string? StockAdjustmentReason = null);
// Burada atomik varyant güncellemesinin bütün satırlarını tek request gövdesinde taşıyorum.
public sealed record BulkUpdateProductVariantsRequest(
    IReadOnlyList<BulkUpdateProductVariantRequestItem> Variants);
// Burada batch içindeki bir varyantın hedef değerleriyle beklenen concurrency tokenını taşıyorum.
public sealed record BulkUpdateProductVariantRequestItem(
    Guid Id,
    [property: Required, MaxLength(150)] string Name,
    [property: Required, MaxLength(150)] string Value,
    [property: Required, MaxLength(100)] string Sku,
    [property: Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal Price,
    [property: Range(0, int.MaxValue)] int Stock,
    Guid ExpectedConcurrencyToken,
    decimal? CompareAtPrice = null,
    [property: MaxLength(100)] string? Barcode = null,
    [property: MaxLength(120)] string? Material = null,
    bool IsActive = true,
    [property: MaxLength(500)] string? StockAdjustmentReason = null);
// Burada varyant fiyat güncelleme isteğini taşıyorum.
public sealed record UpdateVariantPriceRequest(decimal Price, decimal? CompareAtPrice = null);

// Burada varyant SKU'suyla imzalı stok farkını, hareket türünü ve varsa açıklamasını taşıyorum.
public sealed record AdjustStockRequest(
    [property: Required, MaxLength(100)] string ProductVariantSku,
    int QuantityDelta,
    StockMovementType Type,
    string? Reason = null);
