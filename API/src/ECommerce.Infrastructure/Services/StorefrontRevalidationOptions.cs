using System.Text;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

public sealed class StorefrontRevalidationOptions
{
    public const string SectionName = "StorefrontRevalidation";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
}

public sealed class StorefrontRevalidationOptionsValidator : IValidateOptions<StorefrontRevalidationOptions>
{
    private const int MinimumSecretBytes = 32;
    private const int MaximumSecretBytes = 512;

    // Burada yalnız özellik açıkken iç Storefront adresini ve yüksek entropili paylaşılan anahtarı zorunlu tutuyorum.
    public ValidateOptionsResult Validate(string? name, StorefrontRevalidationOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) ||
            !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment) ||
            baseUri.AbsolutePath != "/")
        {
            failures.Add("StorefrontRevalidation:BaseUrl must be an absolute HTTP/HTTPS origin without credentials, path, query, or fragment.");
        }

        var secret = options.Secret ?? string.Empty;
        var secretByteCount = Encoding.UTF8.GetByteCount(secret);
        if (string.IsNullOrWhiteSpace(secret) ||
            secretByteCount < MinimumSecretBytes ||
            secretByteCount > MaximumSecretBytes ||
            secret.Any(character => character is < '!' or > '~'))
        {
            failures.Add($"StorefrontRevalidation:Secret must contain between {MinimumSecretBytes} and {MaximumSecretBytes} printable ASCII bytes without whitespace when revalidation is enabled.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
