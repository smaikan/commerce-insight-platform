using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StorefrontBanners.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.StorefrontBanners.Commands.UpdateStorefrontBanners;

public sealed class UpdateStorefrontBannersCommandHandler
    : IRequestHandler<UpdateStorefrontBannersCommand, StorefrontBannersDto>
{
    private readonly IStorefrontBannerRepository _storefrontBannerRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada banner setini değiştirecek bağımlılıkları hazırlıyorum.
    public UpdateStorefrontBannersCommandHandler(
        IStorefrontBannerRepository storefrontBannerRepository,
        IUnitOfWork unitOfWork)
    {
        _storefrontBannerRepository = storefrontBannerRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ana ve alt banner URL'lerini tek bir atomik banner seti olarak kaydediyorum.
    public async Task<StorefrontBannersDto> Handle(
        UpdateStorefrontBannersCommand request,
        CancellationToken cancellationToken)
    {
        var banners = CreateBanners(request);
        await _storefrontBannerRepository.ReplaceAsync(banners, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return banners.ToDto();
    }

    // Burada istekteki sıralı URL değerlerini sabit banner alanlarına eşliyorum.
    private static IReadOnlyList<StorefrontBanner> CreateBanners(UpdateStorefrontBannersCommand request)
    {
        var banners = new List<StorefrontBanner>(6);
        if (!string.IsNullOrWhiteSpace(request.MainBannerImageUrl))
        {
            banners.Add(new StorefrontBanner(StorefrontBannerSlot.Main, request.MainBannerImageUrl));
        }

        var alternateUrls = request.AltBannerImageUrls ?? [];
        for (var index = 0; index < alternateUrls.Count; index++)
        {
            var slot = (StorefrontBannerSlot)((int)StorefrontBannerSlot.Alternate1 + index);
            banners.Add(new StorefrontBanner(slot, alternateUrls[index]));
        }

        return banners;
    }
}
