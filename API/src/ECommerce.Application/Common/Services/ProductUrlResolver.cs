using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;

namespace ECommerce.Application.Common.Services;

public sealed class ProductUrlResolver : IProductUrlResolver
{
    private const int MaximumUrlLength = 250;
    private readonly IProductRepository _productRepository;
    private readonly IProductUrlGenerator _urlGenerator;

    public ProductUrlResolver(IProductRepository productRepository, IProductUrlGenerator urlGenerator)
    {
        _productRepository = productRepository;
        _urlGenerator = urlGenerator;
    }

    public async Task<string> ResolveAsync(
        string title,
        string? requestedUrl,
        long? excludedProductId = null,
        ISet<string>? requestReservedUrls = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(requestedUrl))
        {
            var explicitUrl = requestedUrl.Trim();
            if (requestReservedUrls?.Contains(explicitUrl) == true ||
                await _productRepository.UrlExistsAsync(explicitUrl, excludedProductId, cancellationToken) ||
                await _productRepository.ReservedUrlExistsAsync(explicitUrl, excludedProductId, cancellationToken))
            {
                throw new ConflictException("Product url already exists or is reserved by a previous product url.");
            }

            requestReservedUrls?.Add(explicitUrl);
            return explicitUrl;
        }

        var baseUrl = _urlGenerator.Generate(title);
        for (var suffix = 1; ; suffix++)
        {
            var suffixText = suffix == 1 ? string.Empty : $"-{suffix}";
            var maximumBaseLength = MaximumUrlLength - suffixText.Length;
            var candidate = baseUrl[..Math.Min(baseUrl.Length, maximumBaseLength)].TrimEnd('-') + suffixText;

            if (requestReservedUrls?.Contains(candidate) == true ||
                await _productRepository.UrlExistsAsync(candidate, excludedProductId, cancellationToken) ||
                await _productRepository.ReservedUrlExistsAsync(candidate, excludedProductId, cancellationToken))
            {
                continue;
            }

            requestReservedUrls?.Add(candidate);
            return candidate;
        }
    }
}
