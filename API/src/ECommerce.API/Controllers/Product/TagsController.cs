using ECommerce.API.Security;
using ECommerce.Application.Tags.Commands.BulkCreateTags;
using ECommerce.Application.Tags.Commands.CreateTag;
using ECommerce.Application.Tags.Commands.DeleteTag;
using ECommerce.Application.Tags.Commands.SetTagActivation;
using ECommerce.Application.Tags.Commands.UpdateTag;
using ECommerce.Application.Tags.Dtos;
using ECommerce.Application.Tags.Queries.GetTagById;
using ECommerce.Application.Tags.Queries.GetTags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/tags")]
public sealed class TagsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada etiket HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public TagsController(ISender sender) => _sender = sender;

    // Burada etiket listesini herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetTagsQuery query, CancellationToken cancellationToken) => Ok(await _sender.Send(query, cancellationToken));

    // Burada tek etiket kaydını herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await _sender.Send(new GetTagByIdQuery(id), cancellationToken));

    // Burada yalnız yöneticinin yeni etiket oluşturmasına izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost]
    public async Task<ActionResult<TagDto>> Create(CreateTagCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));

    // Burada yalnız yöneticinin etiketleri toplu oluşturmasına izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost("bulk")]
    public async Task<ActionResult> BulkCreate(BulkCreateTagsCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));

    // Burada yalnız yöneticinin etiketi ürünleri koruyup bağlantıları kaldırarak silmesine izin veriyorum.
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteTagCommand(id), cancellationToken);
        return NoContent();
    }

    // Burada yalnız yöneticinin etiket alanlarını güncellemesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut("{id:guid}")]
    public async Task<ActionResult<TagDto>> Update(Guid id, TagRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new UpdateTagCommand(id, request.Name, request.Url), cancellationToken));

    // Burada yalnız yöneticinin etiket aktifliğini değiştirmesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<TagDto>> SetActivation(Guid id, SetActivationRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new SetTagActivationCommand(id, request.IsActive), cancellationToken));
}

// Burada etiket güncelleme HTTP gövdesini tanımlıyorum.
public sealed record TagRequest(string Name, string? Url = null);
