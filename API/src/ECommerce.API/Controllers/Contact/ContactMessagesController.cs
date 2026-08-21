using System.ComponentModel.DataAnnotations;
using ECommerce.API.Security;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Contacts;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ECommerce.API.Controllers.Contact;

[ApiController]
[Route("api/contact-messages")]
public sealed class ContactMessagesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IConfiguration _configuration;

    // Burada contact HTTP sınırının MediatR ve güvenilir proxy yapılandırma bağımlılıklarını hazırlıyorum.
    public ContactMessagesController(ISender sender, IConfiguration configuration)
    {
        _sender = sender;
        _configuration = configuration;
    }

    // Burada public iletişim başvurusunu yaklaşık 16 KB sınırında kalıcı kayıt ve outbox akışına iletiyorum.
    [AllowAnonymous]
    [EnableRateLimiting("contact")]
    [RequestSizeLimit(16 * 1024)]
    [HttpPost]
    [ProducesResponseType(typeof(ContactSubmissionReceiptDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ContactSubmissionReceiptDto>> Submit(
        SubmitContactMessageRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required, StringLength(200)] string? idempotencyKey,
        [FromHeader(Name = "X-Turnstile-Token"), StringLength(2048)] string? turnstileToken,
        CancellationToken cancellationToken)
    {
        var trustedClientIp = _configuration.GetValue<bool>("ContactProtection:TrustForwardedClientIp")
            ? HttpContext.Connection.RemoteIpAddress?.ToString()
            : null;
        var receipt = await _sender.Send(
            new SubmitContactMessageCommand(
                request.Name,
                request.Email,
                request.Phone,
                request.Subject,
                request.OrderNumber,
                request.Message,
                idempotencyKey ?? string.Empty,
                turnstileToken,
                trustedClientIp),
            cancellationToken);
        return Accepted(receipt);
    }

    // Burada yöneticinin iletişim mesajlarını bounded filtre ve kararlı pagination ile listelemesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ContactMessageSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<ContactMessageSummaryDto>>> GetList(
        [FromQuery] GetContactMessagesQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    // Burada yöneticinin tam mesaj, audit ve reply teslimat detayını okumasını sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ContactMessageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactMessageDetailDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetContactMessageByIdQuery(id), cancellationToken));

    // Burada yöneticinin optimistic concurrency ile durum değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ContactMessageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContactMessageDetailDto>> ChangeStatus(Guid id, ChangeContactMessageStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new ChangeContactMessageStatusCommand(id, request.Status, request.ExpectedConcurrencyToken), cancellationToken));

    // Burada yöneticinin mevcut users sözleşmesindeki public admin kimliğiyle atama değiştirmesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPatch("{id:guid}/assignment")]
    [ProducesResponseType(typeof(ContactMessageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContactMessageDetailDto>> ChangeAssignment(Guid id, AssignContactMessageRequest request, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new AssignContactMessageCommand(id, request.AssignedAdminUserId, request.ExpectedConcurrencyToken), cancellationToken));

    // Burada yöneticinin append-only dahili not eklemesini sağlıyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{id:guid}/notes")]
    [ProducesResponseType(typeof(ContactMessageDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContactMessageDetailDto>> AddNote(Guid id, AddContactMessageNoteRequest request, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new AddContactMessageNoteCommand(id, request.Note, request.ExpectedConcurrencyToken), cancellationToken));

    // Burada müşteri e-postasını body'den almadan idempotent reply intent'ini outbox'a kabul ediyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{id:guid}/replies")]
    [ProducesResponseType(typeof(ContactMessageDetailDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContactMessageDetailDto>> Reply(
        Guid id,
        ReplyContactMessageRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required, StringLength(200)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var detail = await _sender.Send(
            new ReplyContactMessageCommand(id, request.Body, idempotencyKey ?? string.Empty),
            cancellationToken);
        return Accepted(detail);
    }
}

public sealed record SubmitContactMessageRequest(
    [property: Required, StringLength(150, MinimumLength = 2)] string Name,
    [property: Required, EmailAddress, StringLength(320)] string Email,
    [property: StringLength(30)] string? Phone,
    ContactMessageSubject Subject,
    [property: StringLength(50)] string? OrderNumber,
    [property: Required, StringLength(5000, MinimumLength = 20)] string Message);

public sealed record ChangeContactMessageStatusRequest(ContactMessageStatus Status, Guid ExpectedConcurrencyToken);
public sealed record AssignContactMessageRequest(string? AssignedAdminUserId, Guid ExpectedConcurrencyToken);
public sealed record AddContactMessageNoteRequest([property: Required, StringLength(2000)] string Note, Guid ExpectedConcurrencyToken);
public sealed record ReplyContactMessageRequest([property: Required, StringLength(5000)] string Body);
