using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using System.Net.Mail;

namespace ECommerce.Domain.Entities;

public sealed class StoreSettings : AuditableEntity
{
    public static readonly Guid SingletonId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SeedConcurrencyToken = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly DateTime SeedCreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public const int MaximumDisplayNameLength = 150;
    public const int MaximumShortDescriptionLength = 500;
    public const int MaximumUrlLength = 500;
    public const int MaximumEmailLength = 320;
    public const int MaximumPhoneLength = 30;
    public const int MaximumAddressLength = 1000;
    public const int MaximumWorkingHoursLength = 500;
    public const int MaximumCompanyNameLength = 200;
    public const int MaximumShortTextLength = 150;
    public const int MaximumIdentifierLength = 50;
    public const int MaximumPostalCodeLength = 20;
    public const int MaximumSeoTitleLength = 200;
    public const int MaximumTitleTemplateLength = 250;
    public const int MaximumSeoDescriptionLength = 500;
    public const int MaximumStatusMessageLength = 500;
    public const int MaximumLowStockThreshold = 1_000_000;

    public string DisplayName { get; private set; } = null!;
    public string? ShortDescription { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? DarkLogoUrl { get; private set; }
    public string? FaviconUrl { get; private set; }
    public string? DefaultShareImageUrl { get; private set; }

    public string? SupportEmail { get; private set; }
    public string? SupportPhone { get; private set; }
    public string? WhatsappNumber { get; private set; }
    public string? ContactAddress { get; private set; }
    public string? WorkingHours { get; private set; }
    public string? MapUrl { get; private set; }
    public bool ShowSupportEmail { get; private set; }
    public bool ShowSupportPhone { get; private set; }
    public bool ShowWhatsapp { get; private set; }
    public bool ShowContactAddress { get; private set; }
    public bool ShowWorkingHours { get; private set; }
    public bool ShowMap { get; private set; }

    public string? LegalCompanyName { get; private set; }
    public string? TaxOffice { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? NationalIdentityNumber { get; private set; }
    public string? MersisNumber { get; private set; }
    public string? TradeRegistryNumber { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? AddressLine { get; private set; }
    public string? PostalCode { get; private set; }

    public string? DefaultTitle { get; private set; }
    public string? TitleTemplate { get; private set; }
    public string? DefaultDescription { get; private set; }
    public string? DefaultOpenGraphImageUrl { get; private set; }
    public bool AllowIndexing { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? InstagramUrl { get; private set; }
    public string? TiktokUrl { get; private set; }
    public string? YoutubeUrl { get; private set; }
    public string? XUrl { get; private set; }
    public string? PinterestUrl { get; private set; }

    public StorefrontStatus Status { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool ShowOutOfStockProducts { get; private set; }
    public bool ShowProductsWithoutPrice { get; private set; }
    public StorefrontProductSort DefaultProductSort { get; private set; }
    public bool DefaultProductSortDescending { get; private set; }
    public bool ShowCompareAtPrice { get; private set; }
    public bool ShowStockWarning { get; private set; }
    public int LowStockThreshold { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    // Burada EF Core'un mağaza ayarlarını veri tabanından oluşturmasına izin veriyorum.
    private StoreSettings()
    {
    }

    // Burada güvenli varsayılanlarla tek mağaza ayarı kaydını oluşturuyorum.
    private StoreSettings(Guid concurrencyToken, DateTime createdAtUtc)
    {
        Id = SingletonId;
        CreatedAt = createdAtUtc;
        DisplayName = "Mağaza";
        AllowIndexing = true;
        Status = StorefrontStatus.Active;
        ShowOutOfStockProducts = true;
        ShowProductsWithoutPrice = true;
        DefaultProductSort = StorefrontProductSort.Newest;
        DefaultProductSortDescending = true;
        ShowCompareAtPrice = true;
        ShowStockWarning = false;
        LowStockThreshold = 5;
        ConcurrencyToken = concurrencyToken;
    }

    // Burada kayıt bulunmadığında kullanılacak yeni kalıcı mağaza ayarını oluşturuyorum.
    public static StoreSettings CreateDefault() =>
        new(Guid.NewGuid(), DateTime.UtcNow);

    // Burada migration ve EnsureCreated akışları için deterministik başlangıç kaydını oluşturuyorum.
    public static StoreSettings CreateSeed() =>
        new(SeedConcurrencyToken, SeedCreatedAtUtc);

    // Burada yalnız mağaza kimliği bölümünü diğer ayarları koruyarak güncelliyorum.
    public void UpdateIdentity(
        string displayName,
        string? shortDescription,
        string? logoUrl,
        string? darkLogoUrl,
        string? faviconUrl,
        string? defaultShareImageUrl)
    {
        DisplayName = NormalizeRequired(displayName, MaximumDisplayNameLength, "Display name");
        ShortDescription = NormalizeOptional(shortDescription, MaximumShortDescriptionLength, "Short description");
        LogoUrl = NormalizeHttpUrl(logoUrl, "Logo URL");
        DarkLogoUrl = NormalizeHttpUrl(darkLogoUrl, "Dark logo URL");
        FaviconUrl = NormalizeHttpUrl(faviconUrl, "Favicon URL");
        DefaultShareImageUrl = NormalizeHttpUrl(defaultShareImageUrl, "Default share image URL");
        CompleteUpdate();
    }

    // Burada yalnız iletişim bölümünü görünürlük tercihleriyle birlikte güncelliyorum.
    public void UpdateContact(
        string? supportEmail,
        string? supportPhone,
        string? whatsappNumber,
        string? contactAddress,
        string? workingHours,
        string? mapUrl,
        bool showSupportEmail,
        bool showSupportPhone,
        bool showWhatsapp,
        bool showContactAddress,
        bool showWorkingHours,
        bool showMap)
    {
        SupportEmail = NormalizeEmail(supportEmail);
        SupportPhone = NormalizePhone(supportPhone, "Support phone");
        WhatsappNumber = NormalizePhone(whatsappNumber, "WhatsApp number");
        ContactAddress = NormalizeOptional(contactAddress, MaximumAddressLength, "Contact address");
        WorkingHours = NormalizeOptional(workingHours, MaximumWorkingHoursLength, "Working hours");
        MapUrl = NormalizeHttpUrl(mapUrl, "Map URL");
        ShowSupportEmail = showSupportEmail;
        ShowSupportPhone = showSupportPhone;
        ShowWhatsapp = showWhatsapp;
        ShowContactAddress = showContactAddress;
        ShowWorkingHours = showWorkingHours;
        ShowMap = showMap;
        CompleteUpdate();
    }

    // Burada yalnız mağazanın yasal şirket bilgileri bölümünü güncelliyorum.
    public void UpdateLegal(
        string? legalCompanyName,
        string? taxOffice,
        string? taxNumber,
        string? nationalIdentityNumber,
        string? mersisNumber,
        string? tradeRegistryNumber,
        string? country,
        string? city,
        string? district,
        string? addressLine,
        string? postalCode)
    {
        LegalCompanyName = NormalizeOptional(legalCompanyName, MaximumCompanyNameLength, "Legal company name");
        TaxOffice = NormalizeOptional(taxOffice, MaximumShortTextLength, "Tax office");
        TaxNumber = NormalizeIdentifier(taxNumber, "Tax number");
        NationalIdentityNumber = NormalizeIdentifier(nationalIdentityNumber, "National identity number");
        MersisNumber = NormalizeIdentifier(mersisNumber, "MERSIS number");
        TradeRegistryNumber = NormalizeIdentifier(tradeRegistryNumber, "Trade registry number");
        Country = NormalizeOptional(country, MaximumShortTextLength, "Country");
        City = NormalizeOptional(city, MaximumShortTextLength, "City");
        District = NormalizeOptional(district, MaximumShortTextLength, "District");
        AddressLine = NormalizeOptional(addressLine, MaximumAddressLength, "Address line");
        PostalCode = NormalizeOptional(postalCode, MaximumPostalCodeLength, "Postal code");
        CompleteUpdate();
    }

    // Burada yalnız global SEO ve sosyal bağlantı bölümünü güncelliyorum.
    public void UpdateSeo(
        string? defaultTitle,
        string? titleTemplate,
        string? defaultDescription,
        string? defaultOpenGraphImageUrl,
        bool allowIndexing,
        string? facebookUrl,
        string? instagramUrl,
        string? tiktokUrl,
        string? youtubeUrl,
        string? xUrl,
        string? pinterestUrl)
    {
        DefaultTitle = NormalizeOptional(defaultTitle, MaximumSeoTitleLength, "Default title");
        TitleTemplate = NormalizeTitleTemplate(titleTemplate);
        DefaultDescription = NormalizeOptional(defaultDescription, MaximumSeoDescriptionLength, "Default description");
        DefaultOpenGraphImageUrl = NormalizeHttpUrl(defaultOpenGraphImageUrl, "Default Open Graph image URL");
        AllowIndexing = allowIndexing;
        FacebookUrl = NormalizeHttpUrl(facebookUrl, "Facebook URL");
        InstagramUrl = NormalizeHttpUrl(instagramUrl, "Instagram URL");
        TiktokUrl = NormalizeHttpUrl(tiktokUrl, "TikTok URL");
        YoutubeUrl = NormalizeHttpUrl(youtubeUrl, "YouTube URL");
        XUrl = NormalizeHttpUrl(xUrl, "X URL");
        PinterestUrl = NormalizeHttpUrl(pinterestUrl, "Pinterest URL");
        CompleteUpdate();
    }

    // Burada yalnız storefront davranış tercihleri bölümünü güncelliyorum.
    public void UpdateStorefront(
        StorefrontStatus status,
        string? statusMessage,
        bool showOutOfStockProducts,
        bool showProductsWithoutPrice,
        StorefrontProductSort defaultProductSort,
        bool defaultProductSortDescending,
        bool showCompareAtPrice,
        bool showStockWarning,
        int lowStockThreshold)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException("Storefront status is invalid.");
        }

        if (!Enum.IsDefined(defaultProductSort))
        {
            throw new DomainException("Default product sort is invalid.");
        }

        if (lowStockThreshold is <= 0 or > MaximumLowStockThreshold)
        {
            throw new DomainException($"Low stock threshold must be between 1 and {MaximumLowStockThreshold}.");
        }

        Status = status;
        StatusMessage = NormalizeOptional(statusMessage, MaximumStatusMessageLength, "Status message");
        ShowOutOfStockProducts = showOutOfStockProducts;
        ShowProductsWithoutPrice = showProductsWithoutPrice;
        DefaultProductSort = defaultProductSort;
        DefaultProductSortDescending = defaultProductSortDescending;
        ShowCompareAtPrice = showCompareAtPrice;
        ShowStockWarning = showStockWarning;
        LowStockThreshold = lowStockThreshold;
        CompleteUpdate();
    }

    // Burada başarılı bölüm güncellemesinin zaman ve concurrency token değerlerini yeniliyorum.
    private void CompleteUpdate()
    {
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }

    // Burada zorunlu metni kırpıp uzunluk sınırını uyguluyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        return NormalizeOptional(value, maximumLength, fieldName)!;
    }

    // Burada opsiyonel metni boşsa null, doluysa kırpılmış değer olarak saklıyorum.
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

    // Burada opsiyonel URL'yi yalnız mutlak HTTP veya HTTPS adresi olarak kabul ediyorum.
    private static string? NormalizeHttpUrl(string? value, string fieldName)
    {
        var normalized = NormalizeOptional(value, MaximumUrlLength, fieldName);
        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException($"{fieldName} must be an absolute HTTP/HTTPS URL.");
        }

        return normalized;
    }

    // Burada opsiyonel e-posta adresini standart biçim ve uzunluk kurallarıyla doğruluyorum.
    private static string? NormalizeEmail(string? value)
    {
        var normalized = NormalizeOptional(value, MaximumEmailLength, "Support email");
        if (normalized is null)
        {
            return null;
        }

        if (!MailAddress.TryCreate(normalized, out var address) ||
            !string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("Support email format is invalid.");
        }

        return normalized;
    }

    // Burada telefon değerini ülke varsayımı yapmadan güvenli karakterlerle doğruluyorum.
    private static string? NormalizePhone(string? value, string fieldName)
    {
        var normalized = NormalizeOptional(value, MaximumPhoneLength, fieldName);
        if (normalized is null)
        {
            return null;
        }

        if (normalized.Length < 3 ||
            !normalized.Any(char.IsDigit) ||
            normalized.Any(character => !char.IsDigit(character) && character is not ('+' or '-' or '(' or ')' or ' ' or '.')))
        {
            throw new DomainException($"{fieldName} format is invalid.");
        }

        return normalized;
    }

    // Burada yasal tanımlayıcıyı checksum varsaymadan makul karakterlerle doğruluyorum.
    private static string? NormalizeIdentifier(string? value, string fieldName)
    {
        var normalized = NormalizeOptional(value, MaximumIdentifierLength, fieldName);
        if (normalized is null)
        {
            return null;
        }

        if (normalized.Length < 3 ||
            normalized.Any(character => !char.IsLetterOrDigit(character) && character is not ('-' or '/' or '.' or ' ')))
        {
            throw new DomainException($"{fieldName} format is invalid.");
        }

        return normalized;
    }

    // Burada dolu başlık şablonunun tam bir adet %s yer tutucusu içermesini sağlıyorum.
    private static string? NormalizeTitleTemplate(string? value)
    {
        var normalized = NormalizeOptional(value, MaximumTitleTemplateLength, "Title template");
        if (normalized is null)
        {
            return null;
        }

        var placeholderCount = normalized.Split("%s", StringSplitOptions.None).Length - 1;
        if (placeholderCount != 1)
        {
            throw new DomainException("Title template must contain exactly one %s placeholder.");
        }

        return normalized;
    }
}
