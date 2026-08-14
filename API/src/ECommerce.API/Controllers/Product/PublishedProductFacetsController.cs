using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Queries.GetPublishedProductFacets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace ECommerce.API.Controllers.Product;

[ApiController]
[Route("api/products/published/facets")]
public sealed class PublishedProductFacetsController : ControllerBase
{
    private readonly ISender _sender;

    // Burada public facet isteklerini Application katmanına iletecek göndericiyi hazırlıyorum.
    public PublishedProductFacetsController(ISender sender)
    {
        _sender = sender;
    }

    // Burada marka facetlerini diğer seçili sınıflandırmalara göre adetli getiriyorum.
    [AllowAnonymous]
    [HttpGet("brands")]
    [OutputCache(PolicyName = "public-products")]
    [ProducesResponseType(typeof(IReadOnlyList<PublishedProductFacetItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublishedProductFacetItemDto>>> GetBrands(
        [FromQuery] PublishedProductFacetRequest request,
        CancellationToken cancellationToken) =>
        Ok(await SendAsync(PublishedProductFacetDimension.Brand, request, cancellationToken));

    // Burada koleksiyon facetlerini diğer seçili sınıflandırmalara göre adetli getiriyorum.
    [AllowAnonymous]
    [HttpGet("collections")]
    [OutputCache(PolicyName = "public-products")]
    [ProducesResponseType(typeof(IReadOnlyList<PublishedProductFacetItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublishedProductFacetItemDto>>> GetCollections(
        [FromQuery] PublishedProductFacetRequest request,
        CancellationToken cancellationToken) =>
        Ok(await SendAsync(PublishedProductFacetDimension.Collection, request, cancellationToken));

    // Burada ürün türü facetlerini diğer seçili sınıflandırmalara göre adetli getiriyorum.
    [AllowAnonymous]
    [HttpGet("product-types")]
    [OutputCache(PolicyName = "public-products")]
    [ProducesResponseType(typeof(IReadOnlyList<PublishedProductFacetItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublishedProductFacetItemDto>>> GetProductTypes(
        [FromQuery] PublishedProductFacetRequest request,
        CancellationToken cancellationToken) =>
        Ok(await SendAsync(PublishedProductFacetDimension.ProductType, request, cancellationToken));

    // Burada endpoint boyutunu ortak ve doğrulanabilir Application sorgusuna dönüştürüyorum.
    private Task<IReadOnlyList<PublishedProductFacetItemDto>> SendAsync(
        PublishedProductFacetDimension dimension,
        PublishedProductFacetRequest request,
        CancellationToken cancellationToken) =>
        _sender.Send(new GetPublishedProductFacetsQuery(
            dimension,
            request.TypeId,
            request.BrandId,
            request.CollectionId,
            request.TagId), cancellationToken);
}

// Burada tüm public facet endpointlerinin ortak opsiyonel filtrelerini taşıyorum.
public sealed record PublishedProductFacetRequest(
    Guid? TypeId = null,
    Guid? BrandId = null,
    Guid? CollectionId = null,
    Guid? TagId = null);
