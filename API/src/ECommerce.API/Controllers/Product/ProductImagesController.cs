using ECommerce.API.Security;
using ECommerce.API.Routing;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Images.Commands.CreateProductImage;
using ECommerce.Application.Products.Images.Commands.DeleteProductImage;
using ECommerce.Application.Products.Images.Commands.UpdateProductImage;
using ECommerce.Application.Products.Images.Queries.GetProductImageById;
using ECommerce.Application.Products.Images.Queries.GetProductImagesByProductId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/product-images")]
public sealed class ProductImagesController : ControllerBase
{
    private readonly ISender _sender;
    public ProductImagesController(ISender sender) => _sender = sender;

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductImageDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProductImageByIdQuery(id), cancellationToken));

    [AllowAnonymous]
    [HttpGet("by-product/{productId}")]
    public async Task<ActionResult> GetByProduct(
        string productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetProductImagesByProductIdQuery(
            ApiPublicIdParser.ParseProductId(productId), pageNumber, pageSize), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("by-product/{productId}")]
    public async Task<ActionResult<ProductImageDto>> Create(
        string productId,
        ProductImageRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await _sender.Send(new CreateProductImageCommand(
            ApiPublicIdParser.ParseProductId(productId), request.ImageUrl, request.AltText, request.DisplayOrder, request.IsMain), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductImageDto>> Update(
        Guid id,
        ProductImageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new UpdateProductImageCommand(
            id, request.ImageUrl, request.AltText, request.DisplayOrder, request.IsMain), cancellationToken));

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductImageCommand(id), cancellationToken);
        return NoContent();
    }
}

public sealed record ProductImageRequest(string ImageUrl, string? AltText = null, int DisplayOrder = 0, bool IsMain = false);
