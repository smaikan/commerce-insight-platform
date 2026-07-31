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
[Route("api/accounting/sales-invoices")]
public sealed class SalesInvoicesController : ControllerBase
{
    private readonly ISender _sender;

    // Burada iç satış faturası HTTP operasyonlarını CQRS use case'lerine bağlayacak sender'ı hazırlıyorum.
    public SalesInvoicesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CancellationResultDto>> Cancel(
        Guid id, CancellationRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new CancelSalesInvoiceCommand(id, request.Reason), cancellationToken));

    // Burada doğrudan fatura girdisinden tam olarak bir muhasebe satış siparişi ve bağlı faturayı oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<SalesInvoiceDto>> CreateDirect(
        CreateDirectSalesInvoiceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var invoice = await _sender.Send(
            new CreateDirectSalesInvoiceCommand(
                idempotencyKey ?? string.Empty,
                request.OrderHeader,
                request.InvoiceHeader,
                request.Lines),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    // Burada mevcut muhasebe satış siparişinden daha sonra tek bir iç satış faturası oluşturuyorum.
    [HttpPost("from-order/{accountingSalesOrderId:guid}")]
    public async Task<ActionResult<SalesInvoiceDto>> CreateFromOrder(
        Guid accountingSalesOrderId,
        CreateSalesInvoiceFromOrderRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _sender.Send(
            new CreateSalesInvoiceFromOrderCommand(accountingSalesOrderId, request.Header),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    // Burada taslak faturayı başlık ve gönderilen tam satır listesiyle tek PUT işleminde güncelliyorum.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SalesInvoiceDto>> Update(
        Guid id,
        UpdateSalesInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdateSalesInvoiceCommand(id, request.Header, request.Lines),
            cancellationToken));
    }

    // Burada taslak faturaya seçilen varyanttan güvenilir snapshot taşıyan yeni satır ekliyorum.
    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<SalesInvoiceDto>> AddLine(
        Guid id,
        AddSalesInvoiceLineRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new AddSalesInvoiceLineCommand(id, request.Line),
            cancellationToken));
    }

    // Burada taslak fatura satırının ürün snapshot'ına dokunmadan yalnız ticari alanlarını güncelliyorum.
    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<SalesInvoiceDto>> UpdateLine(
        Guid id,
        Guid lineId,
        UpdateSalesInvoiceLineRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdateSalesInvoiceLineCommand(id, lineId, request.Line),
            cancellationToken));
    }

    // Burada taslak faturadan seçili satırı bağlı sipariş item'ıyla birlikte kaldırıyorum.
    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<SalesInvoiceDto>> RemoveLine(
        Guid id,
        Guid lineId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new RemoveSalesInvoiceLineCommand(id, lineId),
            cancellationToken));
    }

    // Burada iç satış faturasını bağlı muhasebe satış siparişinin posting akışına yönlendiriyorum.
    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<SalesInvoiceDto>> Post(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new PostSalesInvoiceCommand(id),
            cancellationToken));
    }

    // Burada iç satış faturası detayını tarihsel snapshot satırlarıyla getiriyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesInvoiceDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new GetSalesInvoiceByIdQuery(id),
            cancellationToken));
    }

    // Burada iç satış faturalarını güvenli ve sayfalı özetler halinde listeliyorum.
    [HttpGet]
    public async Task<ActionResult<PagedResult<SalesInvoiceSummaryDto>>> GetList(
        [FromQuery] GetSalesInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(query, cancellationToken));
    }
}

// Burada doğrudan fatura girişinin sipariş başlığı, fatura başlığı ve ürün satırlarını taşıyorum.
public sealed record CreateDirectSalesInvoiceRequest(
    AccountingSalesOrderHeaderInput OrderHeader,
    SalesInvoiceHeaderInput InvoiceHeader,
    IReadOnlyList<AccountingSalesOrderLineInput> Lines);

// Burada mevcut muhasebe satış siparişinden üretilecek iç faturanın başlık girdisini taşıyorum.
public sealed record CreateSalesInvoiceFromOrderRequest(SalesInvoiceHeaderInput Header);

// Satırlar gönderilirse liste fatura satırlarının tamamıdır; gönderilmeyen mevcut satırlar kaldırılır.
// Eski istemciler için null satır listesi yalnız başlık güncellemesi olarak geriye dönük desteklenir.
public sealed record UpdateSalesInvoiceRequest(
    SalesInvoiceHeaderInput Header,
    IReadOnlyList<AccountingSalesOrderLineInput>? Lines = null);

// Burada taslak satış faturasına eklenecek yeni varyant satırı girdisini taşıyorum.
public sealed record AddSalesInvoiceLineRequest(AccountingSalesOrderLineInput Line);

// Burada mevcut fatura satırının yalnız değiştirilebilir ticari alanlarını taşıyorum.
public sealed record UpdateSalesInvoiceLineRequest(SalesInvoiceLineUpdateInput Line);
