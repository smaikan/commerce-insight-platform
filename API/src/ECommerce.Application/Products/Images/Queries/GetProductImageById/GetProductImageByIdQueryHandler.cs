using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Images.Queries.GetProductImageById;

public sealed class GetProductImageByIdQueryHandler : IRequestHandler<GetProductImageByIdQuery, ProductImageDto>
{
    private readonly IProductImageRepository _imageRepository;

    public GetProductImageByIdQueryHandler(IProductImageRepository imageRepository)
    {
        _imageRepository = imageRepository;
    }

    // Burada istenen ürün görselini okuyup cevap modeline çeviriyorum.
    public async Task<ProductImageDto> Handle(GetProductImageByIdQuery request, CancellationToken cancellationToken)
    {
        var image = await _imageRepository.GetByIdAsync(request.Id, cancellationToken);

        if (image is null)
        {
            throw new NotFoundException("Product image was not found.");
        }

        return image.ToDto();
    }
}
