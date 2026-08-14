using StoreSettingsEntity = ECommerce.Domain.Entities.StoreSettings;
using System.Net.Mail;

namespace ECommerce.Application.StoreSettings.Commands;

internal static class StoreSettingsValidationRules
{
    // Burada opsiyonel URL'nin mutlak HTTP/HTTPS olup olmadığını doğruluyorum.
    public static bool IsOptionalHttpUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) &&
         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps));

    // Burada opsiyonel e-posta adresinin standart biçimde olup olmadığını doğruluyorum.
    public static bool IsOptionalEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return MailAddress.TryCreate(normalized, out var address) &&
            string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase);
    }

    // Burada opsiyonel telefonu ülkeye özgü varsayım yapmadan güvenli karakterlerle doğruluyorum.
    public static bool IsOptionalPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return normalized.Length is >= 3 and <= StoreSettingsEntity.MaximumPhoneLength &&
            normalized.Any(char.IsDigit) &&
            normalized.All(character => char.IsDigit(character) || character is '+' or '-' or '(' or ')' or ' ' or '.');
    }

    // Burada yasal tanımlayıcıyı checksum varsaymadan makul uzunluk ve karakterlerle doğruluyorum.
    public static bool IsOptionalIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim();
        return normalized.Length is >= 3 and <= StoreSettingsEntity.MaximumIdentifierLength &&
            normalized.All(character => char.IsLetterOrDigit(character) || character is '-' or '/' or '.' or ' ');
    }

    // Burada dolu SEO başlık şablonunun yalnız bir adet %s yer tutucusu taşıdığını doğruluyorum.
    public static bool IsOptionalTitleTemplate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Trim().Split("%s", StringSplitOptions.None).Length - 1 == 1;
    }
}
