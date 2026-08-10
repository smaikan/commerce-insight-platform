using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentValidation;

namespace ECommerce.Application.StorefrontBanners.Commands.ReplaceBannerSection;

public sealed class ReplaceBannerSectionCommandValidator : AbstractValidator<ReplaceBannerSectionCommand>
{
    // Burada bölüm kapasitesini, benzersiz sıralamayı ve main seçimi kurallarını doğruluyorum.
    public ReplaceBannerSectionCommandValidator()
    {
        RuleFor(command => command.Section).IsInEnum();
        RuleFor(command => command.Items)
            .NotNull()
            .Must(items => items is not null && items.Count <= StorefrontBanner.MaximumItemsPerSection)
            .WithMessage("A banner section can contain at most five items.");
        RuleForEach(command => command.Items).SetValidator(new BannerItemInputValidator());
        RuleFor(command => command)
            .Must(HaveUniqueKeys)
            .WithMessage("Banner keys must be unique within a section.");
        RuleFor(command => command)
            .Must(HaveUniqueDisplayOrders)
            .WithMessage("Banner display orders must be unique within a section.");
        RuleFor(command => command)
            .Must(HaveValidMainSelection)
            .WithMessage("A non-empty main banner section must have exactly one active main item; alternate sections cannot have a main item.");
    }

    // Burada bölüm içindeki banner anahtarlarının büyük-küçük harften bağımsız benzersiz olmasını sağlıyorum.
    private static bool HaveUniqueKeys(ReplaceBannerSectionCommand command) =>
        command.Items is not null && command.Items
            .Select(item => item.Key?.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == command.Items.Count;

    // Burada bölüm içindeki sıralama değerlerinin çakışmamasını sağlıyorum.
    private static bool HaveUniqueDisplayOrders(ReplaceBannerSectionCommand command) =>
        command.Items is not null &&
        command.Items.Select(item => item.DisplayOrder).Distinct().Count() == command.Items.Count;

    // Burada yalnız main bölümünde tek ve aktif bir ana banner seçilmesini zorunlu tutuyorum.
    private static bool HaveValidMainSelection(ReplaceBannerSectionCommand command)
    {
        if (command.Items is null)
        {
            return false;
        }

        if (command.Section != StorefrontBannerSection.Main)
        {
            return command.Items.All(item => !item.IsMain);
        }

        if (command.Items.Count == 0)
        {
            return true;
        }

        var selectedItems = command.Items.Where(item => item.IsMain).ToList();
        return selectedItems.Count == 1 && selectedItems[0].IsActive;
    }
}

public sealed class BannerItemInputValidator : AbstractValidator<BannerItemInput>
{
    // Burada banner medya alanlarının uzunluk, biçim ve enum sınırlarını doğruluyorum.
    public BannerItemInputValidator()
    {
        RuleFor(item => item.Name)
            .NotEmpty()
            .MaximumLength(StorefrontBanner.MaximumNameLength);
        RuleFor(item => item.Key)
            .NotEmpty()
            .MaximumLength(StorefrontBanner.MaximumKeyLength)
            .Matches("^[A-Za-z0-9][A-Za-z0-9_-]*$");
        RuleFor(item => item.MediaUrl)
            .NotEmpty()
            .MaximumLength(StorefrontBanner.MaximumUrlLength)
            .Must(BeHttpUrl)
            .WithMessage("Banner media url must be an absolute HTTP or HTTPS URL.");
        RuleFor(item => item.MediaType).IsInEnum();
        RuleFor(item => item.TargetUrl)
            .MaximumLength(StorefrontBanner.MaximumUrlLength)
            .Must(BeSafeTargetUrl)
            .WithMessage("Banner target url must be an application path or an absolute HTTP/HTTPS URL.");
        RuleFor(item => item.AltText)
            .MaximumLength(StorefrontBanner.MaximumAltTextLength);
        RuleFor(item => item.DisplayOrder).GreaterThanOrEqualTo(0);
    }

    // Burada medya adresinin güvenli HTTP veya HTTPS şeması kullanan mutlak URL olmasını kontrol ediyorum.
    private static bool BeHttpUrl(string mediaUrl) =>
        Uri.TryCreate(mediaUrl, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    // Burada opsiyonel hedef adresinin uygulama içi veya güvenli HTTP bağlantısı olmasını doğruluyorum.
    private static bool BeSafeTargetUrl(string? targetUrl)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return true;
        }

        var normalized = targetUrl.Trim();
        return normalized.StartsWith('/') && !normalized.StartsWith("//", StringComparison.Ordinal) ||
            Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
