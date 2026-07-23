using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using MediatR;

namespace ECommerce.Application.Brands.Queries.GetBrands;

public sealed class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, PagedResult<BrandDto>>
{
    private readonly IBrandRepository _brandRepository;

    public GetBrandsQueryHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    // Burada marka listesini okuyup DTO olarak hazırlıyorum.
    public async Task<PagedResult<BrandDto>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var brands = await _brandRepository.GetListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return brands.Map(brand => brand.ToDto());
    }
}
