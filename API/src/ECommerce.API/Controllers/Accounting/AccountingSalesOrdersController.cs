using ECommerce.API.Security;
using ECommerce.Application.Accounting.SalesOrders;
using ECommerce.Application.Accounting.Cancellations;
using ECommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/sales-orders")]
public sealed class AccountingSalesOrdersController : ControllerBase
{
    private readonly ISender _sender;

    // Burada muhasebe satış siparişi HTTP operasyonlarını CQRS use case'lerine bağlayacak sender'ı hazırlıyorum.
    public AccountingSalesOrdersController(ISender sender)
    {
        _sender = sender;
    }

    // Burada Accounting satırlarından stok etkisi olmayan yeni taslak satış siparişi oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<AccountingSalesOrderDto>> Create(
        CreateAccountingSalesOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var order = await _sender.Send(
            new CreateAccountingSalesOrderCommand(
                idempotencyKey ?? string.Empty,
                request.Header,
                request.Lines,
                request.CreateInvoice,
                request.Invoice),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // Burada yalnız taslak muhasebe satış siparişinin başlık ve satırlarını topluca güncelliyorum.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AccountingSalesOrderDto>> Update(
        Guid id,
        UpdateAccountingSalesOrderRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdateAccountingSalesOrderCommand(id, request.Header, request.Lines),
            cancellationToken));
    }

    // Burada taslak muhasebe satış siparişine Accounting isteğinden tek ürün satırı ekliyorum.
    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<AccountingSalesOrderDto>> AddItem(
        Guid id,
        AccountingSalesOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new AddAccountingSalesOrderItemCommand(id, request.Line),
            cancellationToken));
    }

    // Burada taslak muhasebe satış siparişinin seçili ürün satırını güncelliyorum.
    [HttpPut("{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<AccountingSalesOrderDto>> UpdateItem(
        Guid id,
        Guid itemId,
        AccountingSalesOrderItemUpdateRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdateAccountingSalesOrderItemCommand(id, itemId, request.Line),
            cancellationToken));
    }

    // Burada taslak muhasebe satış siparişinden seçili ürün satırını kaldırıyorum.
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<ActionResult<AccountingSalesOrderDto>> RemoveItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new RemoveAccountingSalesOrderItemCommand(id, itemId),
            cancellationToken));
    }

    // Burada muhasebe satış siparişini stok, FIFO ve cari etkileriyle atomik olarak post ediyorum.
    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<AccountingSalesOrderDto>> Post(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new PostAccountingSalesOrderCommand(id),
            cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CancellationResultDto>> Cancel(
        Guid id, CancellationRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new CancelAccountingSalesOrderCommand(id, request.Reason), cancellationToken));

    // Burada muhasebe satış siparişi detayını satır ve stok hareketi bağlantılarıyla getiriyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountingSalesOrderDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new GetAccountingSalesOrderByIdQuery(id),
            cancellationToken));
    }

    // Burada muhasebe satış siparişlerini güvenli ve sayfalı özetler halinde listeliyorum.
    [HttpGet]
    public async Task<ActionResult<PagedResult<AccountingSalesOrderSummaryDto>>> GetList(
        [FromQuery] GetAccountingSalesOrdersQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(query, cancellationToken));
    }
}

// Burada yeni muhasebe satış siparişinin başlık, satır ve isteğe bağlı fatura girdilerini taşıyorum.
public sealed record CreateAccountingSalesOrderRequest(
    AccountingSalesOrderHeaderInput Header,
    IReadOnlyList<AccountingSalesOrderLineInput> Lines,
    bool CreateInvoice,
    SalesInvoiceHeaderInput? Invoice);

// Burada taslak muhasebe satış siparişinin yeni başlık ve satır girdilerini taşıyorum.
public sealed record UpdateAccountingSalesOrderRequest(
    AccountingSalesOrderHeaderInput Header,
    IReadOnlyList<AccountingSalesOrderLineInput> Lines);

// Burada tek muhasebe satış siparişi satırının istemci girdisini taşıyorum.
public sealed record AccountingSalesOrderItemRequest(AccountingSalesOrderLineInput Line);

// Burada mevcut muhasebe satış satırının ürün kimliği dışındaki değiştirilebilir ticari alanlarını taşıyorum.
public sealed record AccountingSalesOrderItemUpdateRequest(SalesInvoiceLineUpdateInput Line);
public sealed record CancellationRequest(string Reason);
