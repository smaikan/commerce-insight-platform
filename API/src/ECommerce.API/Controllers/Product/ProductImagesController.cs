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
using Microsoft.AspNetCore.OutputCaching;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/product-images")]
public sealed class ProductImagesController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IOutputCacheStore _outputCacheStore;

    // Burada ürün görseli HTTP isteklerinin sender ve cache bağımlılıklarını hazırlıyorum.
    public ProductImagesController(ISender sender, IOutputCacheStore outputCacheStore)
    {
        _sender = sender;
        _outputCacheStore = outputCacheStore;
    }

    // Burada tek ürün görselini anonim olarak getiriyorum.
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductImageDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProductImageByIdQuery(id), cancellationToken));

    // Burada ürüne bağlı görselleri anonim ve sayfalı olarak getiriyorum.
    [AllowAnonymous]
    [HttpGet("by-product/{productId}")]
    public async Task<ActionResult> GetByProduct(
        string productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _sender.Send(new GetProductImagesByProductIdQuery(
            ApiPublicIdParser.ParseProductId(productId), pageNumber, pageSize), cancellationToken));

    // Burada yalnız yöneticinin ürüne yeni görsel eklemesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("by-product/{productId}")]
    public async Task<ActionResult<ProductImageDto>> Create(
        string productId,
        ProductImageRequest request,
        CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await SendAndEvictAsync(new CreateProductImageCommand(
            ApiPublicIdParser.ParseProductId(productId), request.ImageUrl, request.AltText, request.DisplayOrder, request.IsMain), cancellationToken));

    // Burada yalnız yöneticinin ürün görselini güncellemesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductImageDto>> Update(
        Guid id,
        ProductImageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await SendAndEvictAsync(new UpdateProductImageCommand(
            id, request.ImageUrl, request.AltText, request.DisplayOrder, request.IsMain), cancellationToken));

    // Burada yalnız yöneticinin ürün görselini silmesine ve ürün cache'ini temizlemesine izin veriyorum.
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductImageCommand(id), cancellationToken);
        await _outputCacheStore.EvictByTagAsync("products", CancellationToken.None);
        return NoContent();
    }

    // Burada ürün görseli değişikliğini çalıştırıp ortak ürün cache etiketini temizliyorum.
    private async Task<ProductImageDto> SendAndEvictAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : IRequest<ProductImageDto>
    {
        var result = await _sender.Send(command, cancellationToken);
        await _outputCacheStore.EvictByTagAsync("products", CancellationToken.None);
        return result;
    }
}

// Burada ürün görseli oluşturma ve güncelleme HTTP gövdesini tanımlıyorum.
public sealed record ProductImageRequest(string ImageUrl, string? AltText = null, int DisplayOrder = 0, bool IsMain = false);
