using ECommerce.API.Security;
using ECommerce.Application.Accounting.Expenses;
using ECommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Accounting;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/accounting/expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly ISender _sender;
    public ExpensesController(ISender sender) => _sender = sender;

    [HttpPost("categories")]
    public async Task<ActionResult<ExpenseCategoryDto>> CreateCategory(CreateExpenseCategoryCommand command, CancellationToken ct)
        => Ok(await _sender.Send(command, ct));
    [HttpGet("categories")]
    public async Task<ActionResult<PagedResult<ExpenseCategoryDto>>> GetCategories([FromQuery] GetExpenseCategoriesQuery query, CancellationToken ct)
        => Ok(await _sender.Send(query, ct));
    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> CreateGeneralExpense(CreateGeneralExpenseCommand command, CancellationToken ct)
        => Ok(await _sender.Send(command, ct));
    [HttpGet]
    public async Task<ActionResult<PagedResult<ExpenseDto>>> GetExpenses([FromQuery] GetExpensesQuery query, CancellationToken ct)
        => Ok(await _sender.Send(query, ct));
}
