using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Brands.Queries.GetBrands;

public sealed class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, IReadOnlyList<BrandDto>>
{
    private readonly IBrandRepository _brandRepository;

    public GetBrandsQueryHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    // Burada marka listesini okuyup DTO olarak hazırlıyorum.
    public async Task<IReadOnlyList<BrandDto>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var brands = await _brandRepository.GetListAsync(cancellationToken);
        return brands.Select(brand => brand.ToDto()).ToList();
    }
}
