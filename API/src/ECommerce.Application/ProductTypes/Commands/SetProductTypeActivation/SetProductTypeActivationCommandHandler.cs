using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.SetProductTypeActivation;

public sealed class SetProductTypeActivationCommandHandler
    : IRequestHandler<SetProductTypeActivationCommand, ProductTypeDto>
{
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetProductTypeActivationCommandHandler(IProductTypeRepository productTypeRepository, IUnitOfWork unitOfWork)
    {
        _productTypeRepository = productTypeRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürün tipinin aktiflik durumunu değiştiriyorum.
    public async Task<ProductTypeDto> Handle(SetProductTypeActivationCommand request, CancellationToken cancellationToken)
    {
        var productType = await _productTypeRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (productType is null)
        {
            throw new NotFoundException("Product type was not found.");
        }

        if (request.IsActive)
        {
            productType.Activate();
        }
        else
        {
            productType.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productType.ToDto();
    }
}
