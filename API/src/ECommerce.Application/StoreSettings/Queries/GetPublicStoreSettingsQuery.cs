using ECommerce.Application.StoreSettings.Dtos;
using MediatR;

namespace ECommerce.Application.StoreSettings.Queries;

// Burada storefront için güvenli mağaza ayarı sorgusunu tanımlıyorum.
public sealed record GetPublicStoreSettingsQuery : IRequest<PublicStoreSettingsDto>;

public sealed class GetPublicStoreSettingsQueryHandler
    : IRequestHandler<GetPublicStoreSettingsQuery, PublicStoreSettingsDto>
{
    private readonly StoreSettingsService _service;

    // Burada public mağaza ayarı sorgusunun Application servisini hazırlıyorum.
    public GetPublicStoreSettingsQueryHandler(StoreSettingsService service)
    {
        _service = service;
    }

    // Burada public mağaza ayarlarını güvenli DTO olarak döndürüyorum.
    public Task<PublicStoreSettingsDto> Handle(
        GetPublicStoreSettingsQuery request,
        CancellationToken cancellationToken) =>
        _service.GetPublicAsync(cancellationToken);
}
