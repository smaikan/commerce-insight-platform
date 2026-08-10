using ECommerce.API.Security;
using ECommerce.Application.Collections.Commands.BulkCreateCollections;
using ECommerce.Application.Collections.Commands.CreateCollection;
using ECommerce.Application.Collections.Commands.SetCollectionActivation;
using ECommerce.Application.Collections.Commands.SetCollectionFeatured;
using ECommerce.Application.Collections.Commands.UpdateCollection;
using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Collections.Queries.GetCollectionById;
using ECommerce.Application.Collections.Queries.GetCollections;
using ECommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/collections")]
public sealed class CollectionsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada koleksiyon HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public CollectionsController(ISender sender) => _sender = sender;

    // Burada koleksiyon listesini herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult<PagedResult<CollectionDto>>> GetList([FromQuery] GetCollectionsQuery query, CancellationToken cancellationToken) => Ok(await _sender.Send(query, cancellationToken));

    // Burada tek koleksiyon kaydını herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet("{id:guid}")]
    public async Task<ActionResult<CollectionDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await _sender.Send(new GetCollectionByIdQuery(id), cancellationToken));

    // Burada yalnız yöneticinin yeni koleksiyon oluşturmasına izin veriyorum.
    [ProducesResponseType(typeof(CollectionDto), StatusCodes.Status201Created)]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost]
    public async Task<ActionResult<CollectionDto>> Create(CreateCollectionCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));

    // Burada yalnız yöneticinin koleksiyonları toplu oluşturmasına izin veriyorum.
    [ProducesResponseType(typeof(IReadOnlyList<CollectionDto>), StatusCodes.Status201Created)]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<CollectionDto>>> BulkCreate(BulkCreateCollectionsCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));

    // Burada yalnız yöneticinin koleksiyon alanlarını güncellemesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut("{id:guid}")]
    public async Task<ActionResult<CollectionDto>> Update(Guid id, CollectionRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new UpdateCollectionCommand(id, request.Name, request.Url, request.Description, request.DisplayOrder, request.ImageUrl), cancellationToken));

    // Burada yalnız yöneticinin koleksiyon aktifliğini değiştirmesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<CollectionDto>> SetActivation(Guid id, SetActivationRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new SetCollectionActivationCommand(id, request.IsActive), cancellationToken));

    // Burada yalnız yöneticinin koleksiyon öne çıkarma durumunu değiştirmesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPatch("{id:guid}/featured")]
    public async Task<ActionResult<CollectionDto>> SetFeatured(Guid id, SetFeaturedRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new SetCollectionFeaturedCommand(id, request.IsFeatured), cancellationToken));
}

// Burada koleksiyon güncelleme HTTP gövdesini tanımlıyorum.
public sealed record CollectionRequest(
    string Name,
    string? Url = null,
    string? Description = null,
    int DisplayOrder = 0,
    string? ImageUrl = null);
