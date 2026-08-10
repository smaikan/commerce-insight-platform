using ECommerce.API.Security;
using ECommerce.Application.Brands.Commands.BulkCreateBrands;
using ECommerce.Application.Brands.Commands.CreateBrand;
using ECommerce.Application.Brands.Commands.SetBrandActivation;
using ECommerce.Application.Brands.Commands.UpdateBrand;
using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Brands.Queries.GetBrandById;
using ECommerce.Application.Brands.Queries.GetBrands;
using ECommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/brands")]
public sealed class BrandsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada marka HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public BrandsController(ISender sender) => _sender = sender;

    // Burada marka listesini herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult<PagedResult<BrandDto>>> GetList([FromQuery] GetBrandsQuery query, CancellationToken cancellationToken) => Ok(await _sender.Send(query, cancellationToken));

    // Burada tek marka kaydını herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet("{id:guid}")]
    public async Task<ActionResult<BrandDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await _sender.Send(new GetBrandByIdQuery(id), cancellationToken));

    // Burada yalnız yöneticinin yeni marka oluşturmasına izin veriyorum.
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost]
    public async Task<ActionResult<BrandDto>> Create(CreateBrandCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));

    // Burada yalnız yöneticinin markaları toplu oluşturmasına izin veriyorum.
    [ProducesResponseType(typeof(IReadOnlyList<BrandDto>), StatusCodes.Status201Created)]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost("bulk")]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> BulkCreate(BulkCreateBrandsCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));

    // Burada yalnız yöneticinin marka alanlarını güncellemesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut("{id:guid}")]
    public async Task<ActionResult<BrandDto>> Update(Guid id, BrandRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new UpdateBrandCommand(id, request.Name, request.Url, request.Description, request.ImageUrl), cancellationToken));

    // Burada yalnız yöneticinin marka aktifliğini değiştirmesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<BrandDto>> SetActivation(Guid id, SetActivationRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new SetBrandActivationCommand(id, request.IsActive), cancellationToken));
}

// Burada marka güncelleme HTTP gövdesini tanımlıyorum.
public sealed record BrandRequest(
    string Name,
    string? Url = null,
    string? Description = null,
    string? ImageUrl = null);
