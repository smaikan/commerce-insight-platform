using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Application.StockMovements.Commands.BulkCreateStockMovements;
using ECommerce.Application.StockMovements.Dtos;
using ECommerce.Application.StockMovements.Queries.GetStockBalance;
using ECommerce.Application.StockMovements.Queries.GetStockMovements;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/stock-movements")]
public sealed class StockMovementsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada stok hareketi HTTP sorgularını Application katmanına iletecek sender'ı hazırlıyorum.
    public StockMovementsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("bulk")]
    // Burada yöneticinin birden çok stok hareketini tek atomik operasyon olarak girmesini sağlıyorum.
    public async Task<ActionResult<BulkCreateStockMovementsResultDto>> CreateBulk(
        BulkCreateStockMovementsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new BulkCreateStockMovementsCommand(
            request.Movements?
                .Select(MapBulkMovement)
                .ToList() ?? []);

        var result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // Burada null JSON öğelerini de validator'ın alan bazlı 400 cevabına dönüştürebileceği modele eşliyorum.
    private static BulkStockMovementItem MapBulkMovement(BulkStockMovementRequest? item)
    {
        return item is null
            ? new BulkStockMovementItem(Guid.Empty, 0, default, null)
            : new BulkStockMovementItem(
                item.ProductVariantId,
                item.QuantityDelta,
                item.Type,
                item.Reason);
    }

    [HttpGet]
    // Burada yöneticinin stok hareketlerini güvenli filtrelerle sayfalı görmesini sağlıyorum.
    public async Task<ActionResult<PagedResult<StockMovementDto>>> GetList(
        [FromQuery] GetStockMovementsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(query, cancellationToken));
    }

    [HttpGet("variants/{productVariantId:guid}/balance")]
    // Burada yöneticinin varyant bakiyesi ile stok hareketi toplamını karşılaştırmasını sağlıyorum.
    public async Task<ActionResult<StockBalanceDto>> GetBalance(
        Guid productVariantId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new GetStockBalanceQuery(productVariantId),
            cancellationToken));
    }
}

// Burada tek transaction içinde işlenecek toplu stok hareketi listesini taşıyorum.
public sealed record BulkCreateStockMovementsRequest(
    IReadOnlyList<BulkStockMovementRequest> Movements);

// Burada toplu listedeki tek varyant hareketinin imzalı miktarını ve varsa açıklamasını taşıyorum.
public sealed record BulkStockMovementRequest(
    Guid ProductVariantId,
    int QuantityDelta,
    StockMovementType Type,
    string? Reason = null);
