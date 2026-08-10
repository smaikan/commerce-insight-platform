using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class StorefrontBanner : AuditableEntity
{
    public const int MaximumItemsPerSection = 5;
    public const int MaximumNameLength = 150;
    public const int MaximumKeyLength = 100;
    public const int MaximumUrlLength = 500;
    public const int MaximumAltTextLength = 500;

    public StorefrontBannerSection Section { get; private set; }
    public string Name { get; private set; } = null!;
    public string Key { get; private set; } = null!;
    public string MediaUrl { get; private set; } = null!;
    public BannerMediaType MediaType { get; private set; }
    public string? TargetUrl { get; private set; }
    public string? AltText { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsMain { get; private set; }

    // Burada EF Core'un banner kaydını veri tabanından oluşturmasına izin veriyorum.
    private StorefrontBanner()
    {
    }

    // Burada banner kaydını bölüm ve medya kurallarını koruyarak oluşturuyorum.
    public StorefrontBanner(
        StorefrontBannerSection section,
        string name,
        string key,
        string mediaUrl,
        BannerMediaType mediaType,
        string? targetUrl,
        string? altText,
        int displayOrder,
        bool isActive,
        bool isMain)
    {
        Section = ValidateSection(section);
        Apply(name, key, mediaUrl, mediaType, targetUrl, altText, displayOrder, isActive, isMain);
    }

    // Burada mevcut banner kaydının yönetilebilir alanlarını topluca değiştiriyorum.
    public void Update(
        string name,
        string mediaUrl,
        BannerMediaType mediaType,
        string? targetUrl,
        string? altText,
        int displayOrder,
        bool isActive,
        bool isMain)
    {
        Apply(name, Key, mediaUrl, mediaType, targetUrl, altText, displayOrder, isActive, isMain);
        MarkAsUpdated();
    }

    // Burada banner alanlarını ortak doğrulamalardan geçirerek kayda uyguluyorum.
    private void Apply(
        string name,
        string key,
        string mediaUrl,
        BannerMediaType mediaType,
        string? targetUrl,
        string? altText,
        int displayOrder,
        bool isActive,
        bool isMain)
    {
        Name = NormalizeRequired(name, MaximumNameLength, "Storefront banner name");
        Key = NormalizeKey(key);
        MediaUrl = NormalizeRequired(mediaUrl, MaximumUrlLength, "Storefront banner media url");
        MediaType = ValidateMediaType(mediaType);
        TargetUrl = NormalizeTargetUrl(targetUrl);
        AltText = NormalizeOptional(altText, MaximumAltTextLength, "Storefront banner alt text");

        if (displayOrder < 0)
        {
            throw new DomainException("Storefront banner display order cannot be negative.");
        }

        if (isMain && Section != StorefrontBannerSection.Main)
        {
            throw new DomainException("Only a main banner section item can be selected as main.");
        }

        DisplayOrder = displayOrder;
        IsActive = isActive;
        IsMain = isMain;
    }

    // Burada banner bölüm değerinin tanımlı olmasını güvenceye alıyorum.
    private static StorefrontBannerSection ValidateSection(StorefrontBannerSection section)
    {
        if (!Enum.IsDefined(section))
        {
            throw new DomainException("Storefront banner section is invalid.");
        }

        return section;
    }

    // Burada medya türünün desteklenen resim veya video değerlerinden biri olmasını sağlıyorum.
    private static BannerMediaType ValidateMediaType(BannerMediaType mediaType)
    {
        if (!Enum.IsDefined(mediaType))
        {
            throw new DomainException("Storefront banner media type is invalid.");
        }

        return mediaType;
    }

    // Burada zorunlu banner metnini kırpıp uzunluk sınırını uyguluyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    // Burada banner anahtarını URL dostu ve küçük harfli sabit kimliğe dönüştürüyorum.
    private static string NormalizeKey(string key)
    {
        var normalized = NormalizeRequired(key, MaximumKeyLength, "Storefront banner key").ToLowerInvariant();
        if (normalized[0] is not (>= 'a' and <= 'z' or >= '0' and <= '9') ||
            !normalized.All(character =>
                character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_'))
        {
            throw new DomainException("Storefront banner key can contain only lowercase letters, numbers, '-' and '_'.");
        }

        return normalized;
    }

    // Burada hedef adresini yalnız uygulama içi yol veya HTTP/HTTPS URL olacak biçimde doğruluyorum.
    private static string? NormalizeTargetUrl(string? targetUrl)
    {
        var normalized = NormalizeOptional(targetUrl, MaximumUrlLength, "Storefront banner target url");
        if (normalized is null)
        {
            return null;
        }

        var isSafeRelativePath = normalized.StartsWith('/') && !normalized.StartsWith("//", StringComparison.Ordinal);
        var isSafeAbsoluteUrl = Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        if (!isSafeRelativePath && !isSafeAbsoluteUrl)
        {
            throw new DomainException("Storefront banner target url must be an application path or an absolute HTTP/HTTPS URL.");
        }

        return normalized;
    }

    // Burada opsiyonel banner metnini boşsa null, doluysa kırpılmış değer olarak saklıyorum.
    private static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }
}
