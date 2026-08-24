using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Returns.Commands.ApproveReturnRequest;
using ECommerce.Application.Returns.Commands.CompleteReturnRequest;
using ECommerce.Application.Returns.Commands.CreateReturnRequest;
using ECommerce.Application.Returns.Commands.ReceiveReturnRequest;
using ECommerce.Application.Returns.Commands.RejectReturnRequest;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Application.Returns.Queries.GetMyReturnRequests;
using ECommerce.Application.Returns.Queries.GetReturnRequestById;
using ECommerce.Application.Returns.Queries.GetReturnRequestByIdForAdmin;
using ECommerce.Application.Returns.Queries.GetReturnRequests;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.Returns;

[ApiController]
[Authorize]
[EnableRateLimiting("orders")]
[Route("api/returns")]
public sealed class ReturnsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada iade HTTP isteklerini Application katmanına iletecek MediatR sender'ını hazırlıyorum.
    public ReturnsController(ISender sender)
    {
        _sender = sender;
    }

    // Burada kullanıcının yalnız kendi iade yaşam döngüsüne uygun siparişi için yeni iade veya değişim talebi oluşturuyorum.
    [HttpPost]
    public async Task<ActionResult<ReturnRequestDto>> Create(
        CreateReturnRequestRequest request,
        CancellationToken cancellationToken)
    {
        var returnRequest = await _sender.Send(
            new CreateReturnRequestCommand(
                request.OrderId,
                request.Type,
                (request.Items ?? [])
                    .Select(item => new CreateReturnItemCommand(
                        item.OrderItemId,
                        item.Quantity,
                        item.ReplacementProductVariantId))
                    .ToList(),
                request.CustomerNote),
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = returnRequest.Id }, returnRequest);
    }

    // Burada kullanıcının kendi iade taleplerini sayfalı şekilde getiriyorum.
    [HttpGet("mine")]
    public async Task<ActionResult<PagedResult<ReturnRequestSummaryDto>>> GetMine(
        [FromQuery] GetMyReturnRequestsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada kullanıcının yalnız kendi iade talebinin ayrıntısını getiriyorum.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReturnRequestDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetReturnRequestByIdQuery(id), cancellationToken));

    // Burada yöneticinin tüm iade taleplerini güvenli filtrelerle sayfalı getirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReturnRequestSummaryDto>>> GetList(
        [FromQuery] GetReturnRequestsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada yöneticinin seçili iade talebinin ayrıntısını görmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("admin/{id:guid}")]
    public async Task<ActionResult<ReturnRequestDto>> GetByIdForAdmin(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetReturnRequestByIdForAdminQuery(id), cancellationToken));

    // Burada yöneticinin fiziksel teslim alınmış iade veya değişim talebini onaylamasını sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ReturnRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnRequestDto>> Approve(
        Guid id,
        ReturnRequestDecisionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ApproveReturnRequestCommand(id, request.DecisionNote), cancellationToken));

    // Burada yöneticinin fiziksel teslim alınmış iade veya değişim talebini reddetmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ReturnRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnRequestDto>> Reject(
        Guid id,
        ReturnRequestDecisionRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new RejectReturnRequestCommand(id, request.DecisionNote), cancellationToken));

    // Burada yöneticinin fiziksel olarak gelen iade veya değişim ürününü teslim almasını sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{id:guid}/receive")]
    [ProducesResponseType(typeof(ReturnRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnRequestDto>> Receive(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ReceiveReturnRequestCommand(id), cancellationToken));

    // Burada yalnız eski yaşam döngüsündeki refund veya exchange kaydının completion uyumluluğunu sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(ReturnRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReturnRequestDto>> Complete(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new CompleteReturnRequestCommand(id), cancellationToken));
}

// Burada müşteri iade veya değişim talebinin HTTP gövdesini tanımlıyorum.
public sealed record CreateReturnRequestRequest(
    Guid OrderId,
    ReturnType Type,
    IReadOnlyList<CreateReturnItemRequest> Items,
    string? CustomerNote = null);

// Burada iade talebindeki tek sipariş kalemi için adet ve değişim varyantı alanlarını tanımlıyorum.
public sealed record CreateReturnItemRequest(
    Guid OrderItemId,
    int Quantity,
    Guid? ReplacementProductVariantId = null);

// Burada yöneticinin onay veya ret gerekçesi olarak girebileceği karar notunu tanımlıyorum.
public sealed record ReturnRequestDecisionRequest(string? DecisionNote = null);
