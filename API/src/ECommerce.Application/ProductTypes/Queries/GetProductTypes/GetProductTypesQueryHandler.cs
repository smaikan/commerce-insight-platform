using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.ProductTypes.Dtos;
using MediatR;

namespace ECommerce.Application.ProductTypes.Queries.GetProductTypes;

public sealed class GetProductTypesQueryHandler : IRequestHandler<GetProductTypesQuery, PagedResult<ProductTypeDto>>
{
    private readonly IProductTypeRepository _productTypeRepository;

    public GetProductTypesQueryHandler(IProductTypeRepository productTypeRepository)
    {
        _productTypeRepository = productTypeRepository;
    }

    // Burada ürün tipi listesini okuyup DTO olarak hazırlıyorum.
    public async Task<PagedResult<ProductTypeDto>> Handle(GetProductTypesQuery request, CancellationToken cancellationToken)
    {
        var productTypes = await _productTypeRepository.GetListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return productTypes.Map(productType => productType.ToDto());
    }
}
