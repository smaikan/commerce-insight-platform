using ECommerce.Application.StoreSettings.Dtos;
using StoreSettingsEntity = ECommerce.Domain.Entities.StoreSettings;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.StoreSettings.Commands.UpdateLegal;

// Burada mağazanın yasal şirket bilgileri güncelleme isteğini taşıyorum.
public sealed record UpdateStoreLegalCommand(
    string? LegalCompanyName,
    string? TaxOffice,
    string? TaxNumber,
    string? NationalIdentityNumber,
    string? MersisNumber,
    string? TradeRegistryNumber,
    string? Country,
    string? City,
    string? District,
    string? AddressLine,
    string? PostalCode,
    Guid ExpectedConcurrencyToken) : IRequest<AdminStoreSettingsDto>;

public sealed class UpdateStoreLegalCommandValidator : AbstractValidator<UpdateStoreLegalCommand>
{
    // Burada yasal şirket alanlarının makul uzunluk ve karakter kurallarını tanımlıyorum.
    public UpdateStoreLegalCommandValidator()
    {
        RuleFor(command => command.LegalCompanyName).MaximumLength(StoreSettingsEntity.MaximumCompanyNameLength);
        RuleFor(command => command.TaxOffice).MaximumLength(StoreSettingsEntity.MaximumShortTextLength);
        ValidateIdentifier(command => command.TaxNumber);
        ValidateIdentifier(command => command.NationalIdentityNumber);
        ValidateIdentifier(command => command.MersisNumber);
        ValidateIdentifier(command => command.TradeRegistryNumber);
        RuleFor(command => command.Country).MaximumLength(StoreSettingsEntity.MaximumShortTextLength);
        RuleFor(command => command.City).MaximumLength(StoreSettingsEntity.MaximumShortTextLength);
        RuleFor(command => command.District).MaximumLength(StoreSettingsEntity.MaximumShortTextLength);
        RuleFor(command => command.AddressLine).MaximumLength(StoreSettingsEntity.MaximumAddressLength);
        RuleFor(command => command.PostalCode).MaximumLength(StoreSettingsEntity.MaximumPostalCodeLength);
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
    }

    // Burada yasal numaralara checksum varsaymadan ortak biçim doğrulamasını uyguluyorum.
    private void ValidateIdentifier(System.Linq.Expressions.Expression<Func<UpdateStoreLegalCommand, string?>> expression) =>
        RuleFor(expression)
            .MaximumLength(StoreSettingsEntity.MaximumIdentifierLength)
            .Must(StoreSettingsValidationRules.IsOptionalIdentifier)
            .WithMessage("Identifier contains unsupported characters or length.");
}

public sealed class UpdateStoreLegalCommandHandler
    : IRequestHandler<UpdateStoreLegalCommand, AdminStoreSettingsDto>
{
    private readonly StoreSettingsService _service;

    // Burada yasal bölümünü güncelleyecek ortak mağaza ayarı servisini hazırlıyorum.
    public UpdateStoreLegalCommandHandler(StoreSettingsService service)
    {
        _service = service;
    }

    // Burada yalnız mağazanın yasal şirket alanlarını beklenen tokenla güncelliyorum.
    public Task<AdminStoreSettingsDto> Handle(
        UpdateStoreLegalCommand request,
        CancellationToken cancellationToken) =>
        _service.UpdateAsync(
            request.ExpectedConcurrencyToken,
            settings => settings.UpdateLegal(
                request.LegalCompanyName,
                request.TaxOffice,
                request.TaxNumber,
                request.NationalIdentityNumber,
                request.MersisNumber,
                request.TradeRegistryNumber,
                request.Country,
                request.City,
                request.District,
                request.AddressLine,
                request.PostalCode),
            cancellationToken);
}
