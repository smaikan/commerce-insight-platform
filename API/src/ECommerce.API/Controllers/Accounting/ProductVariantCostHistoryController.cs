using ECommerce.API.Security;
using ECommerce.Application.Accounting.CostLayers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/product-variants/{productVariantId:guid}/cost-history")]
public sealed class ProductVariantCostHistoryController : ControllerBase
{
    private readonly ISender _sender;

    // Burada varyant maliyet geçmişi API'sini CQRS sorgusuna bağlıyorum.
    public ProductVariantCostHistoryController(ISender sender)
    {
        _sender = sender;
    }

    // Burada seçili varyantın kronolojik maliyet geçmişini yalnız yönetici erişimine sunuyorum.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductVariantCostHistoryDto>>> Get(
        Guid productVariantId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new GetProductVariantCostHistoryQuery(productVariantId),
            cancellationToken));
    }
}
