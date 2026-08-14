using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StoreSettings.Dtos;

namespace ECommerce.Application.StoreSettings;

public sealed class StoreSettingsService
{
    private readonly IStoreSettingsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada mağaza ayarı okuma ve atomik bölüm güncellemelerinin ortak bağımlılıklarını hazırlıyorum.
    public StoreSettingsService(
        IStoreSettingsRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    // Burada public sorgu için kayıt yoksa güvenli varsayılanları veri yazmadan döndürüyorum.
    public async Task<PublicStoreSettingsDto> GetPublicAsync(CancellationToken cancellationToken)
    {
        var settings = await _repository.GetAsync(asTracking: false, cancellationToken)
            ?? ECommerce.Domain.Entities.StoreSettings.CreateDefault();
        return PublicStoreSettingsDto.FromEntity(settings);
    }

    // Burada admin sorgusu için singleton kaydı yoksa oluşturup kalıcı ve kullanılabilir token döndürüyorum.
    public async Task<AdminStoreSettingsDto> GetAdminAsync(CancellationToken cancellationToken)
    {
        var settings = await GetTrackedOrCreateAsync(cancellationToken);
        return AdminStoreSettingsDto.FromEntity(settings);
    }

    // Burada beklenen tokenı doğrulayıp tek ayar bölümünü kaydederek güncel yönetim DTO'sunu döndürüyorum.
    public async Task<AdminStoreSettingsDto> UpdateAsync(
        Guid expectedConcurrencyToken,
        Action<ECommerce.Domain.Entities.StoreSettings> update,
        CancellationToken cancellationToken)
    {
        var settings = await GetTrackedOrCreateAsync(cancellationToken);
        if (settings.ConcurrencyToken != expectedConcurrencyToken)
        {
            throw new ConcurrencyException(
                "Store settings were changed by another operation. Refresh and try again.");
        }

        update(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AdminStoreSettingsDto.FromEntity(settings);
    }

    // Burada eksik singleton kaydını aynı işlem kapsamında oluşturup kalıcılaştırıyorum.
    private async Task<ECommerce.Domain.Entities.StoreSettings> GetTrackedOrCreateAsync(
        CancellationToken cancellationToken)
    {
        var settings = await _repository.GetAsync(asTracking: true, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = ECommerce.Domain.Entities.StoreSettings.CreateDefault();
        _repository.Add(settings);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
