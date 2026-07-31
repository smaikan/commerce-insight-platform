using ECommerce.API.Security;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/current-accounts")]
public sealed class CurrentAccountsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada tek cari hesap master API'sini CQRS use case'lerine bağlıyorum.
    public CurrentAccountsController(ISender sender)
    {
        _sender = sender;
    }

    // Burada yeni cari hesabı oluşturup oluşan kaydın detay adresini döndürüyorum.
    [HttpPost]
    public async Task<ActionResult<CurrentAccountDto>> Create(
        CurrentAccountInput account,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateCurrentAccountCommand(account), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // Burada cari hesap ana bilgilerini ve aktiflik durumunu güncelliyorum.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CurrentAccountDto>> Update(
        Guid id,
        UpdateCurrentAccountRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdateCurrentAccountCommand(id, request.Account, request.IsActive),
            cancellationToken));
    }

    // Burada istenen cari hesabın güncel detayını sorguluyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CurrentAccountDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetCurrentAccountByIdQuery(id), cancellationToken));
    }

    // Burada cari hesapları filtreli ve sayfalı olarak listeliyorum.
    [HttpGet]
    public async Task<ActionResult<PagedResult<CurrentAccountDto>>> GetList(
        [FromQuery] GetCurrentAccountsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(query, cancellationToken));
    }
}

// Burada cari hesap güncelleme isteğinin ana bilgilerini ve aktiflik seçimini birlikte taşıyorum.
public sealed record UpdateCurrentAccountRequest(CurrentAccountInput Account, bool IsActive);
