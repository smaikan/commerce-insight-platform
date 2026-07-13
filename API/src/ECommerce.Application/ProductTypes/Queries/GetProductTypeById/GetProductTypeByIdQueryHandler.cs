using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Queries.GetProductTypeById;

public sealed class GetProductTypeByIdQueryHandler : IRequestHandler<GetProductTypeByIdQuery, ProductTypeDto>
{
    private readonly IProductTypeRepository _productTypeRepository;

    public GetProductTypeByIdQueryHandler(IProductTypeRepository productTypeRepository)
    {
        _productTypeRepository = productTypeRepository;
    }

    // Burada istenen ürün tipini bulup detay cevabına çeviriyorum.
    public async Task<ProductTypeDto> Handle(GetProductTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var productType = await _productTypeRepository.GetByIdAsync(request.Id, cancellationToken);

        if (productType is null)
        {
            throw new NotFoundException("Product type was not found.");
        }

        return productType.ToDto();
    }
}
