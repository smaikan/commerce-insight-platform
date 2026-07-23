using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Images.Commands.CreateProductImage;

public sealed class CreateProductImageCommandHandler : IRequestHandler<CreateProductImageCommand, ProductImageDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductImageRepository _imageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductImageCommandHandler(
        IProductRepository productRepository,
        IProductImageRepository imageRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _imageRepository = imageRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürüne görsel eklemeden önce ürünün gerçekten var olduğunu kontrol ediyorum.
    public async Task<ProductImageDto> Handle(CreateProductImageCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        if (request.IsMain)
        {
            var currentMainImage = await _imageRepository.GetMainByProductIdForUpdateAsync(
                request.ProductId,
                cancellationToken: cancellationToken);
            currentMainImage?.UnsetAsMain();
        }

        var image = new ProductImage(
            request.ProductId,
            request.ImageUrl,
            request.DisplayOrder,
            request.IsMain,
            request.AltText);

        await _imageRepository.AddAsync(image, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return image.ToDto();
    }
}
