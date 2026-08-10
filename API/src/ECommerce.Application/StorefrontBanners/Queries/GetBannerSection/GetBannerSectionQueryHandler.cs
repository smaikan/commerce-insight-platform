using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StorefrontBanners.Dtos;
using MediatR;

namespace ECommerce.Application.StorefrontBanners.Queries.GetBannerSection;

public sealed class GetBannerSectionQueryHandler
    : IRequestHandler<GetBannerSectionQuery, BannerSectionDto>
{
    private readonly IStorefrontBannerRepository _repository;

    // Burada banner bölümü okumasının repository bağımlılığını hazırlıyorum.
    public GetBannerSectionQueryHandler(IStorefrontBannerRepository repository)
    {
        _repository = repository;
    }

    // Burada istenen bölüm kayıtlarını aktiflik kuralıyla sıralı DTO'ya dönüştürüyorum.
    public async Task<BannerSectionDto> Handle(
        GetBannerSectionQuery request,
        CancellationToken cancellationToken)
    {
        var banners = await _repository.GetSectionAsync(
            request.Section,
            request.ActiveOnly,
            cancellationToken);
        return banners.ToDto(request.Section);
    }
}
