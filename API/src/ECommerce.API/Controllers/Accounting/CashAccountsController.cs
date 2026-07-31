using ECommerce.API.Security;
using ECommerce.Application.Accounting.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/cash-accounts")]
public sealed class CashAccountsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada kasa hesabı HTTP operasyonlarını CQRS use case'lerine bağlıyorum.
    public CashAccountsController(ISender sender)
    {
        _sender = sender;
    }

    // Burada bakiyesi ledger'dan türetilecek yeni kasa hesabı oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<CashAccountDto>> Create(
        FinancialAccountInput account,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateCashAccountCommand(account), cancellationToken);
        return CreatedAtAction(nameof(GetStatement), new { id = result.Id }, result);
    }

    // Burada kasa hesaplarını finansal hareketlerden türetilen bakiyeleriyle getiriyorum.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashAccountDto>>> GetList(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetCashAccountsQuery(), cancellationToken));
    }

    // Burada kasa hesabının hareket ve kümülatif bakiye ekstresini getiriyorum.
    [HttpGet("{id:guid}/statement")]
    public async Task<ActionResult<IReadOnlyList<FinancialTransactionDto>>> GetStatement(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetCashAccountStatementQuery(id), cancellationToken));
    }
}
