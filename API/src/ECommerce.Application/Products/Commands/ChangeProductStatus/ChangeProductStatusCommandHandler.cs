using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.ChangeProductStatus;

public sealed class ChangeProductStatusCommandHandler : IRequestHandler<ChangeProductStatusCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeProductStatusCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürünün yayın durumunu değiştiriyorum.
    public async Task<ProductDto> Handle(ChangeProductStatusCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        product.ChangeStatus(request.Status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProduct = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return updatedProduct?.ToDto() ?? product.ToDto();
    }
}
