using ECommerce.API.Security;
using ECommerce.API.Routing;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Variants.Commands.CreateProductVariant;
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

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/product-variants")]
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
    [HttpPatch("{id:guid}/price")]
    // Burada yalnız varyant fiyatlarını yönetici yetkisiyle güncelliyorum.
    public async Task<ActionResult<ProductVariantDto>> UpdatePrice(
        Guid id,
        UpdateVariantPriceRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new UpdateProductVariantPriceCommand(id, request.Price, request.CompareAtPrice), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{id:guid}/stock-movements")]
    // Burada yönetici kaynaklı imzalı stok hareketini türü ve gerekçesiyle kaydediyorum.
    public async Task<ActionResult<ProductVariantDto>> AdjustStock(
        Guid id,
        AdjustStockRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new UpdateProductVariantStockCommand(
            id,
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
// Burada varyant fiyat güncelleme isteğini taşıyorum.
public sealed record UpdateVariantPriceRequest(decimal Price, decimal? CompareAtPrice = null);

// Burada imzalı stok farkını, hareket türünü ve varsa açıklamasını taşıyorum.
public sealed record AdjustStockRequest(
    int QuantityDelta,
    StockMovementType Type,
    string? Reason = null);
