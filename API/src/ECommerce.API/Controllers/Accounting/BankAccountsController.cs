using ECommerce.API.Security;
using ECommerce.Application.Accounting.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/bank-accounts")]
public sealed class BankAccountsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada banka hesabı HTTP operasyonlarını CQRS use case'lerine bağlıyorum.
    public BankAccountsController(ISender sender)
    {
        _sender = sender;
    }

    // Burada bakiyesi ledger'dan türetilecek yeni banka hesabı oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<BankAccountDto>> Create(
        BankAccountInput account,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateBankAccountCommand(account), cancellationToken);
        return CreatedAtAction(nameof(GetStatement), new { id = result.Id }, result);
    }

    // Burada banka hesaplarını finansal hareketlerden türetilen bakiyeleriyle getiriyorum.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankAccountDto>>> GetList(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetBankAccountsQuery(), cancellationToken));
    }

    // Burada banka hesabının hareket ve kümülatif bakiye ekstresini getiriyorum.
    [HttpGet("{id:guid}/statement")]
    public async Task<ActionResult<IReadOnlyList<FinancialTransactionDto>>> GetStatement(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetBankAccountStatementQuery(id), cancellationToken));
    }
}
