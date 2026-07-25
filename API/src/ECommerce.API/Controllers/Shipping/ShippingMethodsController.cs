using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Application.ShippingMethods.Commands.CreateShippingMethod;
using ECommerce.Application.ShippingMethods.Commands.SetShippingMethodActivation;
using ECommerce.Application.ShippingMethods.Commands.UpdateShippingMethod;
using ECommerce.Application.ShippingMethods.Dtos;
using ECommerce.Application.ShippingMethods.Queries.GetShippingMethodById;
using ECommerce.Application.ShippingMethods.Queries.GetShippingMethods;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Shipping;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/shipping-methods")]
public sealed class ShippingMethodsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada kargo yöntemi HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public ShippingMethodsController(ISender sender)
    {
        _sender = sender;
    }

    // Burada checkout ekranlarının yalnız aktif kargo yöntemlerini güvenli sayfalama ile okumasını sağlıyorum.
    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<ActionResult<PagedResult<ShippingMethodDto>>> GetActiveList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var shippingMethods = await _sender.Send(
            new GetShippingMethodsQuery(pageNumber, pageSize, true),
            cancellationToken);
        return Ok(shippingMethods);
    }

    // Burada yöneticinin tüm kargo yöntemlerini isteğe bağlı aktiflik filtresiyle listelemesini sağlıyorum.
    [HttpGet]
    public async Task<ActionResult<PagedResult<ShippingMethodDto>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var shippingMethods = await _sender.Send(
            new GetShippingMethodsQuery(pageNumber, pageSize, isActive),
            cancellationToken);
        return Ok(shippingMethods);
    }

    // Burada yöneticinin tek kargo yöntemi detayını kimliğiyle istemesini iletiyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShippingMethodDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetShippingMethodByIdQuery(id), cancellationToken));
    }

    // Burada yönetici isteğini yeni kargo yöntemi oluşturma komutuna çeviriyorum.
    [HttpPost]
    public async Task<ActionResult<ShippingMethodDto>> Create(
        CreateShippingMethodRequest request,
        CancellationToken cancellationToken)
    {
        var shippingMethod = await _sender.Send(
            new CreateShippingMethodCommand(
                request.Name,
                request.FixedFee,
                request.IsActive,
                request.DisplayOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = shippingMethod.Id }, shippingMethod);
    }

    // Burada rota kimliğiyle gelen kargo yöntemi güncellemesini Application komutuna iletiyorum.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ShippingMethodDto>> Update(
        Guid id,
        UpdateShippingMethodRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdateShippingMethodCommand(id, request.Name, request.FixedFee, request.DisplayOrder),
            cancellationToken));
    }

    // Burada yöneticinin kargo yöntemini yeni checkout seçimlerine açma veya kapatma isteğini iletiyorum.
    [HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<ShippingMethodDto>> SetActivation(
        Guid id,
        SetShippingMethodActivationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new SetShippingMethodActivationCommand(id, request.IsActive),
            cancellationToken));
    }
}

// Burada kargo yöntemi oluşturma için istemciden kabul edilen yönetim alanlarını tanımlıyorum.
public sealed record CreateShippingMethodRequest(
    string Name,
    decimal FixedFee,
    bool IsActive = true,
    int DisplayOrder = 0);

// Burada kargo yöntemi güncelleme için istemciden kabul edilen düzenlenebilir alanları tanımlıyorum.
public sealed record UpdateShippingMethodRequest(string Name, decimal FixedFee, int DisplayOrder);

// Burada kargo yöntemi aktiflik değişikliği için gereken tek HTTP alanını tanımlıyorum.
public sealed record SetShippingMethodActivationRequest(bool IsActive);
