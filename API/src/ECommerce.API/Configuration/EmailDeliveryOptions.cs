using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Configuration;

// Burada parola sıfırlama e-postası için başlangıçta doğrulanacak teslimat ayarlarını tanımlıyorum.
public sealed class EmailDeliveryOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; init; } = string.Empty;
    public string PasswordResetUrl { get; init; } = string.Empty;
    public string ContactInboxAddress { get; init; } = string.Empty;
    public string AdminContactMessageBaseUrl { get; init; } = string.Empty;
    public string SupportReplyToAddress { get; init; } = string.Empty;
    public SmtpDeliveryOptions Smtp { get; init; } = new();
}

// Burada SMTP bağlantısının güvenli başlangıç doğrulamasında kullanılacak alanlarını tanımlıyorum.
public sealed class SmtpDeliveryOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

// Burada hatalı veya güvensiz e-posta ayarlarının production ortamında sessizce devreye girmesini engelliyorum.
public sealed class EmailDeliveryOptionsValidator : IValidateOptions<EmailDeliveryOptions>
{
    private readonly IHostEnvironment _environment;

    // Burada ortam bilgisini production güvenlik kontrollerinde kullanmak üzere saklıyorum.
    public EmailDeliveryOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    // Burada SMTP ve parola sıfırlama bağlantısı ayarlarını hassas değerleri hata mesajına koymadan doğruluyorum.
    public ValidateOptionsResult Validate(string? name, EmailDeliveryOptions options)
    {
        var failures = new List<string>();

        if (!MailAddress.TryCreate(options.FromAddress, out _))
        {
            failures.Add("Email:FromAddress must be a valid email address.");
        }

        if (!MailAddress.TryCreate(options.ContactInboxAddress, out _))
        {
            failures.Add("Email:ContactInboxAddress must be a valid email address.");
        }

        if (!MailAddress.TryCreate(options.SupportReplyToAddress, out _))
        {
            failures.Add("Email:SupportReplyToAddress must be a valid email address.");
        }

        if (!TryGetHttpUrl(options.AdminContactMessageBaseUrl, out var adminContactUri))
        {
            failures.Add("Email:AdminContactMessageBaseUrl must be an absolute HTTP or HTTPS URL.");
        }
        else if (_environment.IsProduction() &&
                 (!string.Equals(adminContactUri!.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || adminContactUri.IsLoopback))
        {
            failures.Add("Email:AdminContactMessageBaseUrl must use non-loopback HTTPS in production.");
        }

        if (string.IsNullOrWhiteSpace(options.Smtp.Host))
        {
            failures.Add("Email:Smtp:Host must be configured.");
        }

        if (options.Smtp.Port is < 1 or > 65_535)
        {
            failures.Add("Email:Smtp:Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(options.Smtp.Username))
        {
            failures.Add("Email:Smtp:Username must be configured.");
        }

        if (!TryGetHttpUrl(options.PasswordResetUrl, out var passwordResetUri))
        {
            failures.Add("Email:PasswordResetUrl must be an absolute HTTP or HTTPS URL.");
        }
        else if (_environment.IsProduction())
        {
            if (!string.Equals(passwordResetUri!.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Email:PasswordResetUrl must use HTTPS in production.");
            }

            if (passwordResetUri.IsLoopback)
            {
                failures.Add("Email:PasswordResetUrl must not target localhost in production.");
            }
        }

        if (_environment.IsProduction() && string.IsNullOrWhiteSpace(options.Smtp.Password))
        {
            failures.Add("Email:Smtp:Password must come from a production secret store.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    // Burada yalnız mutlak HTTP veya HTTPS bağlantılarını kabul ediyorum.
    private static bool TryGetHttpUrl(string value, out Uri? uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out uri) &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        uri = null;
        return false;
    }
}
