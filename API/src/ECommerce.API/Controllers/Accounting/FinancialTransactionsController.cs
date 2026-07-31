using ECommerce.API.Security;
using ECommerce.Application.Accounting.Payments;
using ECommerce.Application.Accounting.Cancellations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/financial-transactions")]
public sealed class FinancialTransactionsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada onaylı manuel kasa ve banka hareketlerini CQRS use case'ine bağlıyorum.
    public FinancialTransactionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<ActionResult<CancellationResultDto>> Reverse(
        Guid id, CancellationRequest request, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new ReverseFinancialTransactionCommand(id, request.Reason), cancellationToken));

    // Burada CashIn, CashOut, transfer, komisyon veya refund ledger hareketi oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<FinancialTransactionDto>> Create(
        CreateFinancialTransactionInput transaction,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencySourceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new CreateFinancialTransactionCommand(idempotencySourceId, transaction),
            cancellationToken));
    }

    // Burada iki banka arasındaki transferi atomik çıkış ve giriş hareketi olarak oluşturuyorum.
    [HttpPost("bank-transfers")]
    public async Task<ActionResult<BankTransferDto>> CreateBankTransfer(
        BankTransferInput transfer,
        [FromHeader(Name = "Idempotency-Key")] Guid idempotencySourceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(
            new CreateBankTransferCommand(idempotencySourceId, transfer),
            cancellationToken));
    }
}
