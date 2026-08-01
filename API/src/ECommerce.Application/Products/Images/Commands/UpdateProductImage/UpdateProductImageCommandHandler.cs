using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Images.Commands.UpdateProductImage;

public sealed class UpdateProductImageCommandHandler : IRequestHandler<UpdateProductImageCommand, ProductImageDto>
{
    private readonly IProductImageRepository _imageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductRepository? _productRepository;

    public UpdateProductImageCommandHandler(
        IProductImageRepository imageRepository,
        IUnitOfWork unitOfWork,
        IProductRepository? productRepository = null)
    {
        _imageRepository = imageRepository;
        _unitOfWork = unitOfWork;
        _productRepository = productRepository;
    }

    // Burada ürün görselini domain kuralından geçirerek güncelliyorum.
    public async Task<ProductImageDto> Handle(UpdateProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _imageRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (image is null)
        {
            throw new NotFoundException("Product image was not found.");
        }

        if (request.IsMain)
        {
            var currentMainImage = await _imageRepository.GetMainByProductIdForUpdateAsync(
                image.ProductId,
                image.Id,
                cancellationToken);
            currentMainImage?.UnsetAsMain();
        }

        var useAutomaticAltText = string.IsNullOrWhiteSpace(request.AltText);
        var altText = request.AltText;
        if (useAutomaticAltText)
        {
            var product = _productRepository is null
                ? null
                : await _productRepository.GetByIdAsync(image.ProductId, cancellationToken);
            if (product is null)
            {
                throw new NotFoundException("Product was not found.");
            }

            altText = product.Title;
        }

        image.Update(request.ImageUrl, altText, request.DisplayOrder, request.IsMain, useAutomaticAltText);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return image.ToDto();
    }
}
