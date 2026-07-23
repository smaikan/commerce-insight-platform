using ECommerce.API.Security;
using ECommerce.Application.ProductTypes.Commands.BulkCreateProductTypes;
using ECommerce.Application.ProductTypes.Commands.CreateProductType;
using ECommerce.Application.ProductTypes.Commands.SetProductTypeActivation;
using ECommerce.Application.ProductTypes.Commands.UpdateProductType;
using ECommerce.Application.ProductTypes.Dtos;
using ECommerce.Application.ProductTypes.Queries.GetProductTypeById;
using ECommerce.Application.ProductTypes.Queries.GetProductTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/product-types")]
public sealed class ProductTypesController : ControllerBase
{
    private readonly ISender _sender;
    public ProductTypesController(ISender sender) => _sender = sender;

    [AllowAnonymous, HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetProductTypesQuery query, CancellationToken cancellationToken) => Ok(await _sender.Send(query, cancellationToken));
    [AllowAnonymous, HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductTypeDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await _sender.Send(new GetProductTypeByIdQuery(id), cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost]
    public async Task<ActionResult<ProductTypeDto>> Create(CreateProductTypeCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost("bulk")]
    public async Task<ActionResult> BulkCreate(BulkCreateProductTypesCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductTypeDto>> Update(Guid id, ProductTypeRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new UpdateProductTypeCommand(id, request.Name, request.Description), cancellationToken));
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<ProductTypeDto>> SetActivation(Guid id, SetActivationRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new SetProductTypeActivationCommand(id, request.IsActive), cancellationToken));
}

public sealed record ProductTypeRequest(string Name, string? Description = null);
