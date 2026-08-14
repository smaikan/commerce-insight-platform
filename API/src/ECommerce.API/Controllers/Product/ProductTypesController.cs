using ECommerce.API.Security;
using ECommerce.Application.ProductTypes.Commands.BulkCreateProductTypes;
using ECommerce.Application.ProductTypes.Commands.CreateProductType;
using ECommerce.Application.ProductTypes.Commands.DeleteProductType;
using ECommerce.Application.ProductTypes.Commands.SetProductTypeActivation;
using ECommerce.Application.ProductTypes.Commands.UpdateProductType;
using ECommerce.Application.ProductTypes.Dtos;
using ECommerce.Application.ProductTypes.Queries.GetProductTypeById;
using ECommerce.Application.ProductTypes.Queries.GetProductTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.API.OutputCaching;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/product-types")]
[ServiceFilter(typeof(ProductOutputCacheInvalidationFilter))]
public sealed class ProductTypesController : ControllerBase
{
    private readonly ISender _sender;

    // Burada ürün türü HTTP isteklerini Application katmanına iletecek sender'ı hazırlıyorum.
    public ProductTypesController(ISender sender) => _sender = sender;

    // Burada ürün türü listesini herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet]
    public async Task<ActionResult> GetList([FromQuery] GetProductTypesQuery query, CancellationToken cancellationToken) => Ok(await _sender.Send(query, cancellationToken));

    // Burada tek ürün türü kaydını herkese açık olarak sunuyorum.
    [AllowAnonymous, HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductTypeDto>> GetById(Guid id, CancellationToken cancellationToken) => Ok(await _sender.Send(new GetProductTypeByIdQuery(id), cancellationToken));

    // Burada yalnız yöneticinin yeni ürün türü oluşturmasına izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost]
    public async Task<ActionResult<ProductTypeDto>> Create(CreateProductTypeCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));

    // Burada yalnız yöneticinin ürün türlerini toplu oluşturmasına izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPost("bulk")]
    public async Task<ActionResult> BulkCreate(BulkCreateProductTypesCommand command, CancellationToken cancellationToken) => StatusCode(201, await _sender.Send(command, cancellationToken));

    // Burada yalnız yöneticinin ürün türünü bağlı ürünleri koruyarak silmesine izin veriyorum.
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteProductTypeCommand(id), cancellationToken);
        return NoContent();
    }

    // Burada yalnız yöneticinin ürün türü alanlarını güncellemesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductTypeDto>> Update(Guid id, ProductTypeRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new UpdateProductTypeCommand(id, request.Name, request.Description), cancellationToken));

    // Burada yalnız yöneticinin ürün türü aktifliğini değiştirmesine izin veriyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly), HttpPatch("{id:guid}/activation")]
    public async Task<ActionResult<ProductTypeDto>> SetActivation(Guid id, SetActivationRequest request, CancellationToken cancellationToken) => Ok(await _sender.Send(new SetProductTypeActivationCommand(id, request.IsActive), cancellationToken));
}

// Burada ürün türü güncelleme HTTP gövdesini tanımlıyorum.
public sealed record ProductTypeRequest(string Name, string? Description = null);
