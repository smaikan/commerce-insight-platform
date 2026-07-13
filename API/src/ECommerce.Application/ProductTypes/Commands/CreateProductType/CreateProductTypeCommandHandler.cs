using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductTypes.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.CreateProductType;

public sealed class CreateProductTypeCommandHandler : IRequestHandler<CreateProductTypeCommand, ProductTypeDto>
{
    private readonly IProductTypeRepository _productTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductTypeCommandHandler(IProductTypeRepository productTypeRepository, IUnitOfWork unitOfWork)
    {
        _productTypeRepository = productTypeRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada yeni ürün tipi oluştururken ismin benzersiz kalmasını sağlıyorum.
    public async Task<ProductTypeDto> Handle(CreateProductTypeCommand request, CancellationToken cancellationToken)
    {
        if (await _productTypeRepository.NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Product type name already exists.");
        }

        var productType = new ProductType(request.Name, request.Description, request.IsActive);

        await _productTypeRepository.AddAsync(productType, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return productType.ToDto();
    }
}
