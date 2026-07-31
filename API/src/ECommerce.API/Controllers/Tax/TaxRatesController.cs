using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Application.TaxRates.Commands.CreateTaxRate;
using ECommerce.Application.TaxRates.Commands.SetTaxRateActivation;
using ECommerce.Application.TaxRates.Commands.UpdateTaxRate;
using ECommerce.Application.TaxRates.Dtos;
using ECommerce.Application.TaxRates.Queries.GetTaxRateById;
using ECommerce.Application.TaxRates.Queries.GetTaxRates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Tax;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/tax-rates")]
public sealed class TaxRatesController : ControllerBase
{
    private readonly ISender _sender;

    // Burada vergi oranı HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public TaxRatesController(ISender sender)
    {
        _sender = sender;
    }

    // Burada ürün ekranlarının yalnız aktif vergi oranlarını güvenli sayfalama ile okumasını sağlıyorum.
    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<ActionResult<PagedResult<TaxRateDto>>> GetActiveList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var taxRates = await _sender.Send(
            new GetTaxRatesQuery(pageNumber, pageSize, true),
            cancellationToken);
        return Ok(taxRates);
    }

    // Burada yöneticinin tüm vergi oranlarını isteğe bağlı aktiflik filtresiyle listelemesini sağlıyorum.
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaxRateDto>>> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var taxRates = await _sender.Send(
            new GetTaxRatesQuery(pageNumber, pageSize, isActive),
            cancellationToken);
        return Ok(taxRates);
    }

    // Burada yöneticinin tek vergi oranı detayını kimliğiyle istemesini iletiyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxRateDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetTaxRateByIdQuery(id), cancellationToken));
    }

    // Burada yönetici isteğini yeni vergi oranı oluşturma komutuna çeviriyorum.
    [HttpPost]
    public async Task<ActionResult<TaxRateDto>> Create(
        CreateTaxRateRequest request,
        CancellationToken cancellationToken)
    {
        var taxRate = await _sender.Send(
            new CreateTaxRateCommand(request.Name, request.Rate, request.IsActive),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = taxRate.Id }, taxRate);
    }

    // Burada rota kimliğiyle gelen vergi oranı güncellemesini Application komutuna iletiyorum.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaxRateDto>> Update(
        Guid id,
        UpdateTaxRateRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdateTaxRateCommand(id, request.Name, request.Rate),
            cancellationToken));
    }

    // Burada yöneticinin vergi oranını yeni ürün seçimlerine açma veya kapatma isteğini iletiyorum.
    [HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<TaxRateDto>> SetActivation(
        Guid id,
        SetTaxRateActivationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new SetTaxRateActivationCommand(id, request.IsActive),
            cancellationToken));
    }
}

// Burada vergi oranı oluşturma için istemciden kabul edilen yönetim alanlarını tanımlıyorum.
public sealed record CreateTaxRateRequest(string Name, decimal Rate, bool IsActive = true);

// Burada vergi oranı güncelleme için istemciden kabul edilen düzenlenebilir alanları tanımlıyorum.
public sealed record UpdateTaxRateRequest(string Name, decimal Rate);

// Burada vergi oranı aktiflik değişikliği için gereken tek HTTP alanını tanımlıyorum.
public sealed record SetTaxRateActivationRequest(bool IsActive);
