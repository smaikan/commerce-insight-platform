using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Brands.Queries.GetBrandById;

public sealed class GetBrandByIdQueryHandler : IRequestHandler<GetBrandByIdQuery, BrandDto>
{
    private readonly IBrandRepository _brandRepository;

    public GetBrandByIdQueryHandler(IBrandRepository brandRepository)
    {
        _brandRepository = brandRepository;
    }

    // Burada istenen markayı bulup detay cevabına çeviriyorum.
    public async Task<BrandDto> Handle(GetBrandByIdQuery request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdAsync(request.Id, cancellationToken);

        if (brand is null)
        {
            throw new NotFoundException("Brand was not found.");
        }

        return brand.ToDto();
    }
}
