using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Payments;

public sealed class IyzicoOptions
{
    public const string SectionName = "Iyzico";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = "https://sandbox-api.iyzipay.com";
    public string ApiKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string CallbackUrl { get; init; } = string.Empty;
    public string ReturnUrl { get; init; } = string.Empty;
    public string SandboxBuyerIdentityNumber { get; init; } = string.Empty;
    public string Country { get; init; } = "Turkey";
    public int[] EnabledInstallments { get; init; } = [1, 2, 3, 6, 9, 12];
}

public sealed class IyzicoOptionsValidator : IValidateOptions<IyzicoOptions>
{
    private static readonly int[] SupportedInstallments = [1, 2, 3, 6, 9, 12];

    // Burada devreye alınmış iyzico sandbox ayarlarının eksiksiz ve güvenli olduğunu başlangıçta doğruluyorum.
    public ValidateOptionsResult Validate(string? name, IyzicoOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(baseUri.Host, "sandbox-api.iyzipay.com", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Iyzico:BaseUrl must be the HTTPS iyzico sandbox API origin.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add("Iyzico:ApiKey is required when iyzico is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            failures.Add("Iyzico:SecretKey is required when iyzico is enabled.");
        }

        ValidateUrl(options.CallbackUrl, "Iyzico:CallbackUrl", failures);
        ValidateUrl(options.ReturnUrl, "Iyzico:ReturnUrl", failures);
        if (options.SandboxBuyerIdentityNumber.Length != 11 ||
            !options.SandboxBuyerIdentityNumber.All(char.IsAsciiDigit))
        {
            failures.Add("Iyzico:SandboxBuyerIdentityNumber must contain exactly 11 digits.");
        }

        if (string.IsNullOrWhiteSpace(options.Country) || options.Country.Length > 100)
        {
            failures.Add("Iyzico:Country is required and cannot exceed 100 characters.");
        }

        if (options.EnabledInstallments.Length == 0 ||
            options.EnabledInstallments.Any(value => !SupportedInstallments.Contains(value)))
        {
            failures.Add("Iyzico:EnabledInstallments may contain only 1, 2, 3, 6, 9, or 12.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    // Burada callback ve dönüş adreslerini mutlak HTTP/HTTPS URL olarak doğruluyorum.
    private static void ValidateUrl(string value, string fieldName, ICollection<string> failures)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add($"{fieldName} must be an absolute HTTP or HTTPS URL.");
        }
    }
}
