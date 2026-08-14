using ECommerce.API.Security;
using ECommerce.Application.StoreSettings.Commands.UpdateContact;
using ECommerce.Application.StoreSettings.Commands.UpdateIdentity;
using ECommerce.Application.StoreSettings.Commands.UpdateLegal;
using ECommerce.Application.StoreSettings.Commands.UpdateSeo;
using ECommerce.Application.StoreSettings.Commands.UpdateStorefront;
using ECommerce.Application.StoreSettings.Dtos;
using ECommerce.Application.StoreSettings.Queries;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.ComponentModel.DataAnnotations;
using StoreSettingsEntity = ECommerce.Domain.Entities.StoreSettings;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/store-settings")]
public sealed class StoreSettingsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IOutputCacheStore _outputCacheStore;

    // Burada mağaza ayarı HTTP isteklerini Application katmanına ve cache invalidation akışına bağlıyorum.
    public StoreSettingsController(ISender sender, IOutputCacheStore outputCacheStore)
    {
        _sender = sender;
        _outputCacheStore = outputCacheStore;
    }

    // Burada storefront'a yalnız anonim erişime uygun güvenli mağaza ayarlarını döndürüyorum.
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<PublicStoreSettingsDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PublicStoreSettingsDto>> GetPublic(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetPublicStoreSettingsQuery(), cancellationToken));

    // Burada yöneticiye bütün düzenlenebilir ayarları güncel concurrency tokenıyla döndürüyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpGet("admin")]
    [ProducesResponseType<AdminStoreSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminStoreSettingsDto>> GetAdmin(CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetAdminStoreSettingsQuery(), cancellationToken));

    // Burada yalnız mağaza kimliği bölümünü atomik olarak güncelliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("identity")]
    [ProducesResponseType<AdminStoreSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<AdminStoreSettingsDto>> UpdateIdentity(
        UpdateStoreIdentityRequest request,
        CancellationToken cancellationToken) =>
        UpdateAndInvalidateAsync(new UpdateStoreIdentityCommand(
            request.DisplayName,
            request.ShortDescription,
            request.LogoUrl,
            request.DarkLogoUrl,
            request.FaviconUrl,
            request.DefaultShareImageUrl,
            request.ExpectedConcurrencyToken), cancellationToken);

    // Burada yalnız mağaza iletişim bölümünü atomik olarak güncelliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("contact")]
    [ProducesResponseType<AdminStoreSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<AdminStoreSettingsDto>> UpdateContact(
        UpdateStoreContactRequest request,
        CancellationToken cancellationToken) =>
        UpdateAndInvalidateAsync(new UpdateStoreContactCommand(
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
            request.ShowMap,
            request.ExpectedConcurrencyToken), cancellationToken);

    // Burada yalnız mağazanın yasal şirket bilgileri bölümünü atomik olarak güncelliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("legal")]
    [ProducesResponseType<AdminStoreSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<AdminStoreSettingsDto>> UpdateLegal(
        UpdateStoreLegalRequest request,
        CancellationToken cancellationToken) =>
        UpdateAndInvalidateAsync(new UpdateStoreLegalCommand(
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
            request.PostalCode,
            request.ExpectedConcurrencyToken), cancellationToken);

    // Burada yalnız global SEO ve sosyal bağlantı bölümünü atomik olarak güncelliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("seo")]
    [ProducesResponseType<AdminStoreSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<AdminStoreSettingsDto>> UpdateSeo(
        UpdateStoreSeoRequest request,
        CancellationToken cancellationToken) =>
        UpdateAndInvalidateAsync(new UpdateStoreSeoCommand(
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
            request.PinterestUrl,
            request.ExpectedConcurrencyToken), cancellationToken);

    // Burada yalnız storefront çalışma durumu ve katalog tercihlerini atomik olarak güncelliyorum.
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPut("storefront")]
    [ProducesResponseType<AdminStoreSettingsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<AdminStoreSettingsDto>> UpdateStorefront(
        UpdateStorefrontPreferencesRequest request,
        CancellationToken cancellationToken) =>
        UpdateAndInvalidateAsync(new UpdateStorefrontPreferencesCommand(
            request.Status,
            request.StatusMessage,
            request.ShowOutOfStockProducts,
            request.ShowProductsWithoutPrice,
            request.DefaultProductSort,
            request.DefaultProductSortDescending,
            request.ShowCompareAtPrice,
            request.ShowStockWarning,
            request.LowStockThreshold,
            request.ExpectedConcurrencyToken), cancellationToken);

    // Burada bölüm güncellemesinden sonra storefront ürün cache'ini temizleyip güncel DTO'yu döndürüyorum.
    private async Task<ActionResult<AdminStoreSettingsDto>> UpdateAndInvalidateAsync(
        IRequest<AdminStoreSettingsDto> command,
        CancellationToken cancellationToken)
    {
        var settings = await _sender.Send(command, cancellationToken);
        await _outputCacheStore.EvictByTagAsync("products", CancellationToken.None);
        return Ok(settings);
    }
}

// Burada kimlik bölümünün typed HTTP isteğini tanımlıyorum.
public sealed record UpdateStoreIdentityRequest(
    [property: Required, StringLength(StoreSettingsEntity.MaximumDisplayNameLength)]
    string DisplayName,
    [property: StringLength(StoreSettingsEntity.MaximumShortDescriptionLength)]
    string? ShortDescription,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? LogoUrl,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? DarkLogoUrl,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? FaviconUrl,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? DefaultShareImageUrl,
    Guid ExpectedConcurrencyToken);

// Burada iletişim bölümünün typed HTTP isteğini tanımlıyorum.
public sealed record UpdateStoreContactRequest(
    [property: StringLength(StoreSettingsEntity.MaximumEmailLength), EmailAddress]
    string? SupportEmail,
    [property: StringLength(StoreSettingsEntity.MaximumPhoneLength)]
    string? SupportPhone,
    [property: StringLength(StoreSettingsEntity.MaximumPhoneLength)]
    string? WhatsappNumber,
    [property: StringLength(StoreSettingsEntity.MaximumAddressLength)]
    string? ContactAddress,
    [property: StringLength(StoreSettingsEntity.MaximumWorkingHoursLength)]
    string? WorkingHours,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? MapUrl,
    bool ShowSupportEmail,
    bool ShowSupportPhone,
    bool ShowWhatsapp,
    bool ShowContactAddress,
    bool ShowWorkingHours,
    bool ShowMap,
    Guid ExpectedConcurrencyToken);

// Burada yasal şirket bilgileri bölümünün typed HTTP isteğini tanımlıyorum.
public sealed record UpdateStoreLegalRequest(
    [property: StringLength(StoreSettingsEntity.MaximumCompanyNameLength)]
    string? LegalCompanyName,
    [property: StringLength(StoreSettingsEntity.MaximumShortTextLength)]
    string? TaxOffice,
    [property: StringLength(StoreSettingsEntity.MaximumIdentifierLength)]
    string? TaxNumber,
    [property: StringLength(StoreSettingsEntity.MaximumIdentifierLength)]
    string? NationalIdentityNumber,
    [property: StringLength(StoreSettingsEntity.MaximumIdentifierLength)]
    string? MersisNumber,
    [property: StringLength(StoreSettingsEntity.MaximumIdentifierLength)]
    string? TradeRegistryNumber,
    [property: StringLength(StoreSettingsEntity.MaximumShortTextLength)]
    string? Country,
    [property: StringLength(StoreSettingsEntity.MaximumShortTextLength)]
    string? City,
    [property: StringLength(StoreSettingsEntity.MaximumShortTextLength)]
    string? District,
    [property: StringLength(StoreSettingsEntity.MaximumAddressLength)]
    string? AddressLine,
    [property: StringLength(StoreSettingsEntity.MaximumPostalCodeLength)]
    string? PostalCode,
    Guid ExpectedConcurrencyToken);

// Burada SEO ve sosyal bağlantı bölümünün typed HTTP isteğini tanımlıyorum.
public sealed record UpdateStoreSeoRequest(
    [property: StringLength(StoreSettingsEntity.MaximumSeoTitleLength)]
    string? DefaultTitle,
    [property: StringLength(StoreSettingsEntity.MaximumTitleTemplateLength)]
    string? TitleTemplate,
    [property: StringLength(StoreSettingsEntity.MaximumSeoDescriptionLength)]
    string? DefaultDescription,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? DefaultOpenGraphImageUrl,
    bool AllowIndexing,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? FacebookUrl,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? InstagramUrl,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? TiktokUrl,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? YoutubeUrl,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? XUrl,
    [property: StringLength(StoreSettingsEntity.MaximumUrlLength)]
    string? PinterestUrl,
    Guid ExpectedConcurrencyToken);

// Burada storefront çalışma durumu ve katalog tercihlerinin typed HTTP isteğini tanımlıyorum.
public sealed record UpdateStorefrontPreferencesRequest(
    StorefrontStatus Status,
    [property: StringLength(StoreSettingsEntity.MaximumStatusMessageLength)]
    string? StatusMessage,
    bool ShowOutOfStockProducts,
    bool ShowProductsWithoutPrice,
    StorefrontProductSort DefaultProductSort,
    bool DefaultProductSortDescending,
    bool ShowCompareAtPrice,
    bool ShowStockWarning,
    [property: Range(1, StoreSettingsEntity.MaximumLowStockThreshold)]
    int LowStockThreshold,
    Guid ExpectedConcurrencyToken);
