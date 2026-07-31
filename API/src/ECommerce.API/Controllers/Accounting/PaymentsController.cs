using ECommerce.API.Security;
using ECommerce.Application.Accounting.Payments;
using ECommerce.Application.Accounting.Cancellations;
using ECommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada ödeme ve tahsilat HTTP operasyonlarını CQRS use case'lerine bağlıyorum.
    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    // Burada cari hareket tahsisli müşteri tahsilatı veya tedarikçi ödemesi oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create(
        CreatePaymentInput payment,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreatePaymentCommand(idempotencyKey ?? string.Empty, payment),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // Burada tek ödemenin tahsis ve finans hesabı detayını getiriyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetPaymentByIdQuery(id), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<CancellationResultDto>> Cancel(
        Guid id, CancellationRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new CancelPaymentCommand(id, request.Reason), cancellationToken));

    // Burada ödemeleri güvenli ve sayfalı özetler halinde getiriyorum.
    [HttpGet]
    public async Task<ActionResult<PagedResult<PaymentSummaryDto>>> GetList(
        [FromQuery] GetPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(query, cancellationToken));
    }
}
