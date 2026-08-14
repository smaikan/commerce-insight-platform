using ECommerce.Application.StoreSettings.Dtos;
using StoreSettingsEntity = ECommerce.Domain.Entities.StoreSettings;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.StoreSettings.Commands.UpdateIdentity;

// Burada mağaza kimliği bölümünün atomik güncelleme isteğini taşıyorum.
public sealed record UpdateStoreIdentityCommand(
    string DisplayName,
    string? ShortDescription,
    string? LogoUrl,
    string? DarkLogoUrl,
    string? FaviconUrl,
    string? DefaultShareImageUrl,
    Guid ExpectedConcurrencyToken) : IRequest<AdminStoreSettingsDto>;

public sealed class UpdateStoreIdentityCommandValidator : AbstractValidator<UpdateStoreIdentityCommand>
{
    // Burada mağaza kimliği alanlarının uzunluk, zorunluluk ve URL kurallarını tanımlıyorum.
    public UpdateStoreIdentityCommandValidator()
    {
        RuleFor(command => command.DisplayName).NotEmpty().MaximumLength(StoreSettingsEntity.MaximumDisplayNameLength);
        RuleFor(command => command.ShortDescription).MaximumLength(StoreSettingsEntity.MaximumShortDescriptionLength);
        ValidateUrl(command => command.LogoUrl);
        ValidateUrl(command => command.DarkLogoUrl);
        ValidateUrl(command => command.FaviconUrl);
        ValidateUrl(command => command.DefaultShareImageUrl);
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
    }

    // Burada kimlik bölümündeki opsiyonel URL alanlarına ortak kuralları uyguluyorum.
    private void ValidateUrl(System.Linq.Expressions.Expression<Func<UpdateStoreIdentityCommand, string?>> expression) =>
        RuleFor(expression)
            .MaximumLength(StoreSettingsEntity.MaximumUrlLength)
            .Must(StoreSettingsValidationRules.IsOptionalHttpUrl)
            .WithMessage("URL must be an absolute HTTP/HTTPS URL.");
}

public sealed class UpdateStoreIdentityCommandHandler
    : IRequestHandler<UpdateStoreIdentityCommand, AdminStoreSettingsDto>
{
    private readonly StoreSettingsService _service;

    // Burada kimlik bölümünü güncelleyecek ortak mağaza ayarı servisini hazırlıyorum.
    public UpdateStoreIdentityCommandHandler(StoreSettingsService service)
    {
        _service = service;
    }

    // Burada yalnız mağaza kimliği alanlarını beklenen tokenla güncelliyorum.
    public Task<AdminStoreSettingsDto> Handle(
        UpdateStoreIdentityCommand request,
        CancellationToken cancellationToken) =>
        _service.UpdateAsync(
            request.ExpectedConcurrencyToken,
            settings => settings.UpdateIdentity(
                request.DisplayName,
                request.ShortDescription,
                request.LogoUrl,
                request.DarkLogoUrl,
                request.FaviconUrl,
                request.DefaultShareImageUrl),
            cancellationToken);
}
