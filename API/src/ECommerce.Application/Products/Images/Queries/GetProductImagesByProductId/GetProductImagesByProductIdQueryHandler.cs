using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Images.Queries.GetProductImagesByProductId;

public sealed class GetProductImagesByProductIdQueryHandler : IRequestHandler<GetProductImagesByProductIdQuery, PagedResult<ProductImageDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _imageRepository;

    public GetProductImagesByProductIdQueryHandler(
        IProductRepository productRepository,
        IProductImageRepository imageRepository)
    {
        _productRepository = productRepository;
        _imageRepository = imageRepository;
    }

    // Burada bir ürüne ait görselleri sıralı şekilde liste cevabına çeviriyorum.
    public async Task<PagedResult<ProductImageDto>> Handle(GetProductImagesByProductIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        var images = await _imageRepository.GetByProductIdAsync(
            request.ProductId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return images.Map(image => image.ToDto());
    }
}
