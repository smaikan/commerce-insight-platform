using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.UpdateProductType;

public sealed class UpdateProductTypeCommandHandler : IRequestHandler<UpdateProductTypeCommand, ProductTypeDto>
{
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductTypeCommandHandler(IProductTypeRepository productTypeRepository, IUnitOfWork unitOfWork)
    {
        _productTypeRepository = productTypeRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürün tipi bilgisini güncellemeden önce kaydı ve isim çakışmasını kontrol ediyorum.
    public async Task<ProductTypeDto> Handle(UpdateProductTypeCommand request, CancellationToken cancellationToken)
    {
        var productType = await _productTypeRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (productType is null)
        {
            throw new NotFoundException("Product type was not found.");
        }

        if (await _productTypeRepository.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            throw new ConflictException("Product type name already exists.");
        }

        productType.Rename(request.Name);
        productType.SetDescription(request.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productType.ToDto();
    }
}
