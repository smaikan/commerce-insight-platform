using ECommerce.API.Security;
using ECommerce.Application.Tags.Commands.BulkCreateTags;
using ECommerce.Application.Tags.Commands.CreateTag;
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
    public TagsController(ISender sender) => _sender = sender;

    [AllowAnonymous, HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetTagsQuery query, CancellationToken cancellationToken) => Ok(await _sender.Send(query, cancellationToken));
    [AllowAnonymous, HttpGet("{id:guid}")]
    public async Task<ActionResult<TagDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await _sender.Send(new GetTagByIdQuery(id), cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost]
    public async Task<ActionResult<TagDto>> Create(CreateTagCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost("bulk")]
    public async Task<ActionResult> BulkCreate(BulkCreateTagsCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut("{id:guid}")]
    public async Task<ActionResult<TagDto>> Update(Guid id, TagRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new UpdateTagCommand(id, request.Name, request.Url), cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<TagDto>> SetActivation(Guid id, SetActivationRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new SetTagActivationCommand(id, request.IsActive), cancellationToken));
}

public sealed record TagRequest(string Name, string? Url = null);
