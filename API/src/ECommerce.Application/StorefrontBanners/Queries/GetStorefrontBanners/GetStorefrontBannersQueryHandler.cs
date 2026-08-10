using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StorefrontBanners.Dtos;
using MediatR;

namespace ECommerce.Application.StorefrontBanners.Queries.GetStorefrontBanners;

public sealed class GetStorefrontBannersQueryHandler
    : IRequestHandler<GetStorefrontBannersQuery, StorefrontBannersDto>
{
    private readonly IStorefrontBannerRepository _storefrontBannerRepository;

    // Burada storefront banner okuma bağımlılığını hazırlıyorum.
    public GetStorefrontBannersQueryHandler(IStorefrontBannerRepository storefrontBannerRepository)
    {
        _storefrontBannerRepository = storefrontBannerRepository;
    }

    // Burada banner kayıtlarını sıralı storefront sözleşmesine dönüştürüyorum.
    public async Task<StorefrontBannersDto> Handle(
        GetStorefrontBannersQuery request,
        CancellationToken cancellationToken)
    {
        var banners = await _storefrontBannerRepository.GetAllAsync(cancellationToken);
        return banners.ToDto();
    }
}
