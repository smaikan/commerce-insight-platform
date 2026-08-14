using ECommerce.Application.StoreSettings.Dtos;
using MediatR;

namespace ECommerce.Application.StoreSettings.Queries;

// Burada yönetim için tam mağaza ayarı sorgusunu tanımlıyorum.
public sealed record GetAdminStoreSettingsQuery : IRequest<AdminStoreSettingsDto>;

public sealed class GetAdminStoreSettingsQueryHandler
    : IRequestHandler<GetAdminStoreSettingsQuery, AdminStoreSettingsDto>
{
    private readonly StoreSettingsService _service;

    // Burada admin mağaza ayarı sorgusunun Application servisini hazırlıyorum.
    public GetAdminStoreSettingsQueryHandler(StoreSettingsService service)
    {
        _service = service;
    }

    // Burada bütün yönetilebilir mağaza ayarlarını güncel tokenla döndürüyorum.
    public Task<AdminStoreSettingsDto> Handle(
        GetAdminStoreSettingsQuery request,
        CancellationToken cancellationToken) =>
        _service.GetAdminAsync(cancellationToken);
}
