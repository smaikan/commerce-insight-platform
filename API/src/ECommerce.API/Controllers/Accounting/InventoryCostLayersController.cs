using ECommerce.API.Security;
using ECommerce.Application.Accounting.CostLayers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/inventory-cost-layers")]
public sealed class InventoryCostLayersController : ControllerBase
{
    private readonly ISender _sender;

    // Burada açılış maliyet katmanı HTTP operasyonlarını CQRS use case'lerine yönlendirecek sender'ı hazırlıyorum.
    public InventoryCostLayersController(ISender sender)
    {
        _sender = sender;
    }

    // Burada varyantın OpeningBalance maliyet katmanını güncel miktar ve concurrency token bilgisiyle getiriyorum.
    [HttpGet("opening-balance/by-variant/{productVariantId:guid}")]
    public async Task<ActionResult<OpeningBalanceCostLayerDto>> GetOpeningBalance(
        Guid productVariantId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new GetOpeningBalanceCostLayerByVariantQuery(productVariantId),
            cancellationToken));
    }

    // Burada yalnız OpeningBalance katmanının kalan miktarına uygulanacak gelecekteki maliyeti güncelliyorum.
    [HttpPatch("{id:guid}/opening-balance-cost")]
    public async Task<ActionResult<OpeningBalanceCostLayerDto>> UpdateOpeningBalanceCost(
        Guid id,
        UpdateOpeningBalanceCostLayerRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdateOpeningBalanceCostLayerCommand(
                id,
                request.UnitCostExcludingVat,
                request.UnitCostIncludingVat,
                request.ExpectedConcurrencyToken),
            cancellationToken));
    }
}

// Burada OpeningBalance katmanının kalan stok maliyeti ve beklenen eşzamanlılık anahtarını taşıyorum.
public sealed record UpdateOpeningBalanceCostLayerRequest(
    Guid ExpectedConcurrencyToken,
    decimal UnitCostExcludingVat = 0m,
    decimal? UnitCostIncludingVat = null);
