using ECommerce.Application.StoreSettings.Dtos;
using StoreSettingsEntity = ECommerce.Domain.Entities.StoreSettings;
using FluentValidation;
using MediatR;

namespace ECommerce.Application.StoreSettings.Commands.UpdateSeo;

// Burada global SEO ve sosyal bağlantı bölümünün atomik güncelleme isteğini taşıyorum.
public sealed record UpdateStoreSeoCommand(
    string? DefaultTitle,
    string? TitleTemplate,
    string? DefaultDescription,
    string? DefaultOpenGraphImageUrl,
    bool AllowIndexing,
    string? FacebookUrl,
    string? InstagramUrl,
    string? TiktokUrl,
    string? YoutubeUrl,
    string? XUrl,
    string? PinterestUrl,
    Guid ExpectedConcurrencyToken) : IRequest<AdminStoreSettingsDto>;

public sealed class UpdateStoreSeoCommandValidator : AbstractValidator<UpdateStoreSeoCommand>
{
    // Burada SEO metinlerinin, %s şablonunun ve sosyal URL'lerin kurallarını tanımlıyorum.
    public UpdateStoreSeoCommandValidator()
    {
        RuleFor(command => command.DefaultTitle).MaximumLength(StoreSettingsEntity.MaximumSeoTitleLength);
        RuleFor(command => command.TitleTemplate)
            .MaximumLength(StoreSettingsEntity.MaximumTitleTemplateLength)
            .Must(StoreSettingsValidationRules.IsOptionalTitleTemplate)
            .WithMessage("TitleTemplate must contain exactly one %s placeholder when provided.");
        RuleFor(command => command.DefaultDescription).MaximumLength(StoreSettingsEntity.MaximumSeoDescriptionLength);
        ValidateUrl(command => command.DefaultOpenGraphImageUrl);
        ValidateUrl(command => command.FacebookUrl);
        ValidateUrl(command => command.InstagramUrl);
        ValidateUrl(command => command.TiktokUrl);
        ValidateUrl(command => command.YoutubeUrl);
        ValidateUrl(command => command.XUrl);
        ValidateUrl(command => command.PinterestUrl);
        RuleFor(command => command.ExpectedConcurrencyToken).NotEmpty();
    }

    // Burada SEO bölümündeki opsiyonel URL alanlarına ortak kuralları uyguluyorum.
    private void ValidateUrl(System.Linq.Expressions.Expression<Func<UpdateStoreSeoCommand, string?>> expression) =>
        RuleFor(expression)
            .MaximumLength(StoreSettingsEntity.MaximumUrlLength)
            .Must(StoreSettingsValidationRules.IsOptionalHttpUrl)
            .WithMessage("URL must be an absolute HTTP/HTTPS URL.");
}

public sealed class UpdateStoreSeoCommandHandler
    : IRequestHandler<UpdateStoreSeoCommand, AdminStoreSettingsDto>
{
    private readonly StoreSettingsService _service;

    // Burada SEO bölümünü güncelleyecek ortak mağaza ayarı servisini hazırlıyorum.
    public UpdateStoreSeoCommandHandler(StoreSettingsService service)
    {
        _service = service;
    }

    // Burada yalnız global SEO ve sosyal bağlantı alanlarını beklenen tokenla güncelliyorum.
    public Task<AdminStoreSettingsDto> Handle(
        UpdateStoreSeoCommand request,
        CancellationToken cancellationToken) =>
        _service.UpdateAsync(
            request.ExpectedConcurrencyToken,
            settings => settings.UpdateSeo(
                request.DefaultTitle,
                request.TitleTemplate,
                request.DefaultDescription,
                request.DefaultOpenGraphImageUrl,
                request.AllowIndexing,
                request.FacebookUrl,
                request.InstagramUrl,
                request.TiktokUrl,
                request.YoutubeUrl,
                request.XUrl,
                request.PinterestUrl),
            cancellationToken);
}
