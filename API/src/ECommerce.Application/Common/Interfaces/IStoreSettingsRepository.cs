namespace ECommerce.Application.Common.Interfaces;

public interface IStoreSettingsRepository
{
    // Burada tek mağaza ayarı kaydını izleme tercihiyle okuma sözleşmesini tanımlıyorum.
    Task<ECommerce.Domain.Entities.StoreSettings?> GetAsync(
        bool asTracking,
        CancellationToken cancellationToken = default);

    // Burada kayıt bulunmadığında oluşturulan tek mağaza ayarını ekleme sözleşmesini tanımlıyorum.
    void Add(ECommerce.Domain.Entities.StoreSettings settings);
}
