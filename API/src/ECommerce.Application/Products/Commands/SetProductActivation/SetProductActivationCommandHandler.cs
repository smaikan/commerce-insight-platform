using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.SetProductActivation;

public sealed class SetProductActivationCommandHandler : IRequestHandler<SetProductActivationCommand, ProductDto>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetProductActivationCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürünün aktiflik durumunu değiştiriyorum.
    public async Task<ProductDto> Handle(SetProductActivationCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        if (request.IsActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updatedProduct = await _productRepository.GetByIdAsync(product.Id, cancellationToken);
        return updatedProduct?.ToDto() ?? product.ToDto();
    }
}
