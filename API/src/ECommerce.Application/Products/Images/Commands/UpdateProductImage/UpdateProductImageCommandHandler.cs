using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Images.Commands.UpdateProductImage;

public sealed class UpdateProductImageCommandHandler : IRequestHandler<UpdateProductImageCommand, ProductImageDto>
{
    private readonly IProductImageRepository _imageRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductImageCommandHandler(IProductImageRepository imageRepository, IUnitOfWork unitOfWork)
    {
        _imageRepository = imageRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürün görselini domain kuralından geçirerek güncelliyorum.
    public async Task<ProductImageDto> Handle(UpdateProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _imageRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (image is null)
        {
            throw new NotFoundException("Product image was not found.");
        }

        image.Update(request.ImageUrl, request.AltText, request.DisplayOrder, request.IsMain);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return image.ToDto();
    }
}
