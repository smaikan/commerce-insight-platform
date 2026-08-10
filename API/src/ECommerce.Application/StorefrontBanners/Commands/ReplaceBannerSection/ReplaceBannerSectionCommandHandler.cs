using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StorefrontBanners.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.StorefrontBanners.Commands.ReplaceBannerSection;

public sealed class ReplaceBannerSectionCommandHandler
    : IRequestHandler<ReplaceBannerSectionCommand, BannerSectionDto>
{
    private readonly IStorefrontBannerRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada bölüm güncellemesinin repository ve transaction bağımlılıklarını hazırlıyorum.
    public ReplaceBannerSectionCommandHandler(
        IStorefrontBannerRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    // Burada bölüm öğelerini istenen sıraya göre normalize edip tek işlemde kalıcılaştırıyorum.
    public async Task<BannerSectionDto> Handle(
        ReplaceBannerSectionCommand request,
        CancellationToken cancellationToken)
    {
        // Burada aynı bölümdeki eşzamanlı tam-set güncellemelerini serializable transaction ile sıralıyorum.
        return await _unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCancellationToken =>
        {
            var banners = CreateOrderedBanners(request);
            await _repository.ReplaceSectionAsync(request.Section, banners, transactionCancellationToken);
            await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
            var persisted = await _repository.GetSectionAsync(
                request.Section,
                activeOnly: false,
                transactionCancellationToken);
            return persisted.ToDto(request.Section);
        }, cancellationToken);
    }

    // Burada seçili main kaydını başa, kalan kayıtları displayOrder sırasına yerleştiriyorum.
    private static IReadOnlyList<StorefrontBanner> CreateOrderedBanners(ReplaceBannerSectionCommand request)
    {
        return request.Items
            .OrderByDescending(item => item.IsMain)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .Select((item, index) => new StorefrontBanner(
                request.Section,
                item.Name,
                item.Key,
                item.MediaUrl,
                item.MediaType,
                item.TargetUrl,
                item.AltText,
                index,
                item.IsActive,
                item.IsMain))
            .ToList();
    }
}
