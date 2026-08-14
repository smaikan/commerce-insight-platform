using ECommerce.Application.StoreSettings.Dtos;
using StoreSettingsEntity = ECommerce.Domain.Entities.StoreSettings;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.StoreSettings.Commands.UpdateContact;

// Burada mağaza iletişim bölümünün atomik güncelleme isteğini taşıyorum.
public sealed record UpdateStoreContactCommand(
    string? SupportEmail,
    string? SupportPhone,
    string? WhatsappNumber,
    string? ContactAddress,
    string? WorkingHours,
    string? MapUrl,
    bool ShowSupportEmail,
    bool ShowSupportPhone,
    bool ShowWhatsapp,
    bool ShowContactAddress,
    bool ShowWorkingHours,
    bool ShowMap,
    Guid ExpectedConcurrencyToken) : IRequest<AdminStoreSettingsDto>;

public sealed class UpdateStoreContactCommandValidator : AbstractValidator<UpdateStoreContactCommand>
{
    // Burada iletişim değerlerinin biçim ve uzunluk kurallarını tanımlıyorum.
    public UpdateStoreContactCommandValidator()
    {
        RuleFor(command => command.SupportEmail)
            .MaximumLength(StoreSettingsEntity.MaximumEmailLength)
            .Must(StoreSettingsValidationRules.IsOptionalEmail)
            .WithMessage("SupportEmail must be a valid email address.");
        ValidatePhone(command => command.SupportPhone);
        ValidatePhone(command => command.WhatsappNumber);
        RuleFor(command => command.ContactAddress).MaximumLength(StoreSettingsEntity.MaximumAddressLength);
        RuleFor(command => command.WorkingHours).MaximumLength(StoreSettingsEntity.MaximumWorkingHoursLength);
        RuleFor(command => command.MapUrl)
            .MaximumLength(StoreSettingsEntity.MaximumUrlLength)
            .Must(StoreSettingsValidationRules.IsOptionalHttpUrl)
            .WithMessage("MapUrl must be an absolute HTTP/HTTPS URL.");
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
    }

    // Burada iletişim telefonlarına ülkeye özel olmayan ortak doğrulamayı uyguluyorum.
    private void ValidatePhone(System.Linq.Expressions.Expression<Func<UpdateStoreContactCommand, string?>> expression) =>
        RuleFor(expression)
            .MaximumLength(StoreSettingsEntity.MaximumPhoneLength)
            .Must(StoreSettingsValidationRules.IsOptionalPhone)
            .WithMessage("Phone value contains unsupported characters or length.");
}

public sealed class UpdateStoreContactCommandHandler
    : IRequestHandler<UpdateStoreContactCommand, AdminStoreSettingsDto>
{
    private readonly StoreSettingsService _service;

    // Burada iletişim bölümünü güncelleyecek ortak mağaza ayarı servisini hazırlıyorum.
    public UpdateStoreContactCommandHandler(StoreSettingsService service)
    {
        _service = service;
    }

    // Burada yalnız mağaza iletişim ve görünürlük alanlarını beklenen tokenla güncelliyorum.
    public Task<AdminStoreSettingsDto> Handle(
        UpdateStoreContactCommand request,
        CancellationToken cancellationToken) =>
        _service.UpdateAsync(
            request.ExpectedConcurrencyToken,
            settings => settings.UpdateContact(
                request.SupportEmail,
                request.SupportPhone,
                request.WhatsappNumber,
                request.ContactAddress,
                request.WorkingHours,
                request.MapUrl,
                request.ShowSupportEmail,
                request.ShowSupportPhone,
                request.ShowWhatsapp,
                request.ShowContactAddress,
                request.ShowWorkingHours,
                request.ShowMap),
            cancellationToken);
}
