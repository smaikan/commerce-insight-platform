using ECommerce.API.Security;
using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Application.Accounting.Expenses;
using ECommerce.Domain.Accounting.Expenses;
using ECommerce.Application.Accounting.Cancellations;
using ECommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/purchase-invoices")]
public sealed class PurchaseInvoicesController : ControllerBase
{
    private readonly ISender _sender;

    // Burada alış faturası HTTP operasyonlarını CQRS use case'lerine yönlendirecek sender'ı hazırlıyorum.
    public PurchaseInvoicesController(ISender sender)
    {
        _sender = sender;
    }

    // Burada fiziksel stok etkisi olmayan yeni taslak alış faturası oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<PurchaseInvoiceDto>> Create(
        CreatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var invoice = await _sender.Send(
            new CreatePurchaseInvoiceCommand(request.Header, request.Lines),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    // Burada yalnız taslak alış faturasının başlık ve satırlarını güncelliyorum.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> Update(
        Guid id,
        UpdatePurchaseInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdatePurchaseInvoiceCommand(id, request.Header, request.Lines),
            cancellationToken));
    }

    // Burada taslak faturaya yeni ürün satırı ekliyorum.
    [HttpPost("{id:guid}/lines")]
    public async Task<ActionResult<PurchaseInvoiceDto>> AddLine(
        Guid id,
        PurchaseInvoiceLineInput line,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new AddPurchaseInvoiceLineCommand(id, line), cancellationToken));
    }

    // Burada taslak faturadaki ürün satırını güvenilir snapshot ile değiştiriyorum.
    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> UpdateLine(
        Guid id,
        Guid lineId,
        PurchaseInvoiceLineCommercialUpdateInput line,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new UpdatePurchaseInvoiceLineCommand(id, lineId, line),
            cancellationToken));
    }

    // Burada taslak faturadan seçili satırı kaldırıyorum.
    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> RemoveLine(
        Guid id,
        Guid lineId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new RemovePurchaseInvoiceLineCommand(id, lineId), cancellationToken));
    }

    // Burada mevcut pozitif Purchase hareketlerini fatura satırına kısmi olarak tahsis ediyorum.
    [HttpPut("{id:guid}/lines/{lineId:guid}/allocations")]
    public async Task<ActionResult<PurchaseInvoiceDto>> SetAllocations(
        Guid id,
        Guid lineId,
        IReadOnlyList<PurchaseInvoiceAllocationInput> allocations,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new SetPurchaseInvoiceAllocationsCommand(id, lineId, allocations),
            cancellationToken));
    }

    // Burada alış faturasını stok hareketi oluşturmadan atomik olarak post ediyorum.
    [HttpPost("{id:guid}/post")]
    public async Task<ActionResult<PurchaseInvoiceDto>> Post(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new PostPurchaseInvoiceCommand(id), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CancellationResultDto>> Cancel(
        Guid id, CancellationRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new CancelPurchaseInvoiceCommand(id, request.Reason), cancellationToken));

    [HttpPost("{id:guid}/expenses")]
    public async Task<ActionResult<PurchaseInvoiceExpenseDto>> AddExpense(
        Guid id, AddPurchaseInvoiceExpenseRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new AddPurchaseInvoiceExpenseCommand(id, request.CategoryId,
            request.AllocationMethod, request.AmountExcludingVat, request.VatRate,
            request.Description, request.ManualAllocations), cancellationToken));

    [HttpGet("{id:guid}/expenses")]
    public async Task<ActionResult<IReadOnlyList<PurchaseInvoiceExpenseDto>>> GetExpenses(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetPurchaseInvoiceExpensesQuery(id), cancellationToken));

    // Burada alış faturası detayını bütün satır ve allocation kayıtlarıyla getiriyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseInvoiceDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetPurchaseInvoiceByIdQuery(id), cancellationToken));
    }

    // Burada alış faturalarını sayfalı özetler halinde listeliyorum.
    [HttpGet]
    public async Task<ActionResult<PagedResult<PurchaseInvoiceSummaryDto>>> GetList(
        [FromQuery] GetPurchaseInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(query, cancellationToken));
    }

    // Burada varyant için henüz tamamen maliyetlendirilmemiş Purchase hareketlerini getiriyorum.
    [HttpGet("available-stock-movements")]
    public async Task<ActionResult<IReadOnlyList<AvailableStockMovementDto>>> GetAvailableStockMovements(
        [FromQuery] Guid productVariantId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new GetAvailablePurchaseStockMovementsQuery(productVariantId),
            cancellationToken));
    }
}

// Burada yeni taslak alış faturasının başlık ve başlangıç satırlarını taşıyorum.
public sealed record CreatePurchaseInvoiceRequest(
    PurchaseInvoiceHeaderInput Header,
    IReadOnlyList<PurchaseInvoiceLineInput> Lines);

// Burada taslak alış faturasının toplu başlık ve satır güncellemesini taşıyorum.
public sealed record UpdatePurchaseInvoiceRequest(
    PurchaseInvoiceHeaderInput Header,
    IReadOnlyList<PurchaseInvoiceLineInput> Lines);

public sealed record AddPurchaseInvoiceExpenseRequest(Guid CategoryId,
    PurchaseExpenseAllocationMethod AllocationMethod, decimal AmountExcludingVat,
    decimal VatRate, string? Description,
    IReadOnlyList<ManualExpenseAllocationInput>? ManualAllocations);
