using System.Net.Http.Json;
using ECommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

// Burada Admin Panel işlemlerinden sonra Storefront (Next.js) API'sine on-demand cache revalidation bildirimi gönderiyorum.
public sealed class StorefrontRevalidationService : IStorefrontRevalidationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StorefrontRevalidationService> _logger;
    private readonly bool _enabled;
    private readonly string _secret;
    private readonly Uri? _endpoint;

    public StorefrontRevalidationService(
        HttpClient httpClient,
        IOptions<StorefrontRevalidationOptions> options,
        ILogger<StorefrontRevalidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _enabled = options.Value.Enabled;
        _secret = options.Value.Secret;
        _endpoint = _enabled
            ? new Uri(new Uri(options.Value.BaseUrl.TrimEnd('/') + "/"), "api/revalidate")
            : null;
    }

    public async Task RevalidateAsync(string? tag = null, string? path = null, CancellationToken cancellationToken = default)
    {
        if (!_enabled || _endpoint is null)
        {
            _logger.LogDebug("Storefront revalidation devre dışı; tag={Tag}, path={Path}", tag, path);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.Add("x-revalidate-secret", _secret);
            request.Content = JsonContent.Create(new { tag, path });

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            using var response = await _httpClient.SendAsync(request, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Storefront cache başarıyla temizlendi: tag={Tag}, path={Path}", tag, path);
            }
            else
            {
                _logger.LogWarning("Storefront cache temizleme isteği başarısız oldu ({StatusCode}): tag={Tag}", response.StatusCode, tag);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Storefront revalidation isteği zaman aşımına uğradı: tag={Tag}, path={Path}", tag, path);
        }
        catch (Exception ex)
        {
            // Storefront geçici olarak kapalı olsa bile başarıyla tamamlanmış ana mutasyonu geriye dönük 500'e çevirmiyorum.
            _logger.LogWarning(ex, "Storefront revalidation bildirimi gönderilemedi: tag={Tag}, path={Path}", tag, path);
        }
    }

    public Task RevalidateProductsAsync(CancellationToken cancellationToken = default)
        => RevalidateAsync(tag: "products", path: "/products", cancellationToken);

    public Task RevalidateBannersAsync(CancellationToken cancellationToken = default)
        => RevalidateAsync(tag: "banners", path: "/", cancellationToken);

    public Task RevalidateStoreSettingsAsync(CancellationToken cancellationToken = default)
        => RevalidateAsync(tag: "store-settings", path: "/", cancellationToken);

    public Task RevalidateCategoriesAsync(CancellationToken cancellationToken = default)
        => RevalidateAsync(tag: "published-product-types", path: "/categories", cancellationToken);

    public Task RevalidateCollectionsAsync(CancellationToken cancellationToken = default)
        => RevalidateAsync(tag: "published-collections", path: "/collections", cancellationToken);
}
