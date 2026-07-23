using ECommerce.API.Security;
using ECommerce.Application.Brands.Commands.BulkCreateBrands;
using ECommerce.Application.Brands.Commands.CreateBrand;
using ECommerce.Application.Brands.Commands.SetBrandActivation;
using ECommerce.Application.Brands.Commands.UpdateBrand;
using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Brands.Queries.GetBrandById;
using ECommerce.Application.Brands.Queries.GetBrands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/brands")]
public sealed class BrandsController : ControllerBase
{
    private readonly ISender _sender;
    public BrandsController(ISender sender) => _sender = sender;

    [AllowAnonymous, HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetBrandsQuery query, CancellationToken cancellationToken) => Ok(await _sender.Send(query, cancellationToken));
    [AllowAnonymous, HttpGet("{id:guid}")]
    public async Task<ActionResult<BrandDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await _sender.Send(new GetBrandByIdQuery(id), cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost]
    public async Task<ActionResult<BrandDto>> Create(CreateBrandCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost("bulk")]
    public async Task<ActionResult> BulkCreate(BulkCreateBrandsCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut("{id:guid}")]
    public async Task<ActionResult<BrandDto>> Update(Guid id, BrandRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new UpdateBrandCommand(id, request.Name, request.Url, request.Description), cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<BrandDto>> SetActivation(Guid id, SetActivationRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new SetBrandActivationCommand(id, request.IsActive), cancellationToken));
}

public sealed record BrandRequest(string Name, string? Url = null, string? Description = null);
