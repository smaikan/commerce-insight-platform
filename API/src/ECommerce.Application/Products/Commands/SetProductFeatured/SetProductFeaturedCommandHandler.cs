using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.SetProductFeatured;

public sealed class SetProductFeaturedCommandHandler : IRequestHandler<SetProductFeaturedCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetProductFeaturedCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürünün öne çıkarılma durumunu değiştiriyorum.
    public async Task<ProductDto> Handle(SetProductFeaturedCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        if (request.IsFeatured)
        {
            product.MarkAsFeatured();
        }
        else
        {
            product.UnmarkAsFeatured();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProduct = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return updatedProduct?.ToDto() ?? product.ToDto();
    }
}
