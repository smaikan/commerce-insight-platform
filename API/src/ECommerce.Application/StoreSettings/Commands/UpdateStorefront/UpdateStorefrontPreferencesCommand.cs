using ECommerce.Application.StoreSettings.Dtos;
using StoreSettingsEntity = ECommerce.Domain.Entities.StoreSettings;
using ECommerce.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.StoreSettings.Commands.UpdateStorefront;

// Burada storefront çalışma durumu ve katalog tercihleri güncelleme isteğini taşıyorum.
public sealed record UpdateStorefrontPreferencesCommand(
    StorefrontStatus Status,
    string? StatusMessage,
    bool ShowOutOfStockProducts,
    bool ShowProductsWithoutPrice,
    StorefrontProductSort DefaultProductSort,
    bool DefaultProductSortDescending,
    bool ShowCompareAtPrice,
    bool ShowStockWarning,
    int LowStockThreshold,
    Guid ExpectedConcurrencyToken) : IRequest<AdminStoreSettingsDto>;

public sealed class UpdateStorefrontPreferencesCommandValidator
    : AbstractValidator<UpdateStorefrontPreferencesCommand>
{
    // Burada storefront enum, mesaj ve düşük stok eşiği kurallarını tanımlıyorum.
    public UpdateStorefrontPreferencesCommandValidator()
    {
        RuleFor(command => command.Status).IsInEnum();
        RuleFor(command => command.StatusMessage).MaximumLength(StoreSettingsEntity.MaximumStatusMessageLength);
        RuleFor(command => command.DefaultProductSort).IsInEnum();
        RuleFor(command => command.LowStockThreshold)
            .InclusiveBetween(1, StoreSettingsEntity.MaximumLowStockThreshold);
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
    }
}

public sealed class UpdateStorefrontPreferencesCommandHandler
    : IRequestHandler<UpdateStorefrontPreferencesCommand, AdminStoreSettingsDto>
{
    private readonly StoreSettingsService _service;

    // Burada storefront bölümünü güncelleyecek ortak mağaza ayarı servisini hazırlıyorum.
    public UpdateStorefrontPreferencesCommandHandler(StoreSettingsService service)
    {
        _service = service;
    }

    // Burada yalnız storefront davranış tercihlerini beklenen tokenla güncelliyorum.
    public Task<AdminStoreSettingsDto> Handle(
        UpdateStorefrontPreferencesCommand request,
        CancellationToken cancellationToken) =>
        _service.UpdateAsync(
            request.ExpectedConcurrencyToken,
            settings => settings.UpdateStorefront(
                request.Status,
                request.StatusMessage,
                request.ShowOutOfStockProducts,
                request.ShowProductsWithoutPrice,
                request.DefaultProductSort,
                request.DefaultProductSortDescending,
                request.ShowCompareAtPrice,
                request.ShowStockWarning,
                request.LowStockThreshold),
            cancellationToken);
}
