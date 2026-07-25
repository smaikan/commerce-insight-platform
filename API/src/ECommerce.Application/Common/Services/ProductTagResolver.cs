using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Application.Common.Services;

public sealed class ProductTagResolver : IProductTagResolver
{
    private const int MaximumTagUrlLength = 200;
    private readonly ITagRepository _tagRepository;
    private readonly IUrlGenerator _urlGenerator;

    // Burada etiket çözümleme servisinin repository ve URL üreticisi bağımlılıklarını hazırlıyorum.
    public ProductTagResolver(
        ITagRepository tagRepository,
        IUrlGenerator urlGenerator)
    {
        _tagRepository = tagRepository;
        _urlGenerator = urlGenerator;
    }

    // Burada mevcut etiketleri yeniden kullanıp eksik etiketleri aynı işleme kayda hazırlıyorum.
    public async Task<ProductTagResolution> ResolveAsync(
        IEnumerable<string>? tagNames,
        CancellationToken cancellationToken = default)
    {
        var preparedTags = (tagNames ?? [])
            .Where(tagName => !string.IsNullOrWhiteSpace(tagName))
            .Select(tagName =>
            {
                var name = tagName.Trim();
                var baseUrl = _urlGenerator.Generate(name);
                return new PreparedTag(name, baseUrl, CreateCollisionUrl(baseUrl, name));
            })
            .ToList();

        if (preparedTags.Count == 0)
        {
            return ProductTagResolution.Empty;
        }

        var existingTags = await _tagRepository.GetByNamesOrUrlsAsync(
            preparedTags.Select(tag => tag.Name),
            preparedTags.SelectMany(tag => new[] { tag.BaseUrl, tag.CollisionUrl }),
            cancellationToken);
        var tagsByName = existingTags
            .ToDictionary(tag => tag.Name, StringComparer.OrdinalIgnoreCase);
        var occupiedUrls = existingTags
            .Select(tag => tag.Url)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var resolvedTagIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var newTags = new List<Tag>();

        foreach (var preparedTag in preparedTags)
        {
            if (resolvedTagIds.ContainsKey(preparedTag.Name))
            {
                continue;
            }

            if (!tagsByName.TryGetValue(preparedTag.Name, out var tag))
            {
                var url = occupiedUrls.Contains(preparedTag.BaseUrl)
                    ? preparedTag.CollisionUrl
                    : preparedTag.BaseUrl;
                if (occupiedUrls.Contains(url))
                {
                    url = CreateRandomCollisionUrl(preparedTag.BaseUrl);
                }

                tag = new Tag(preparedTag.Name, url);
                newTags.Add(tag);
                tagsByName[tag.Name] = tag;
                occupiedUrls.Add(tag.Url);
            }

            resolvedTagIds[preparedTag.Name] = tag.Id;
        }

        if (newTags.Count > 0)
        {
            await _tagRepository.AddRangeAsync(newTags, cancellationToken);
        }

        return new ProductTagResolution(resolvedTagIds);
    }

    // Burada aynı slugı üreten farklı etiket adları için kararlı ve kısa bir URL oluşturuyorum.
    private static string CreateCollisionUrl(string baseUrl, string tagName)
    {
        var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(tagName.ToUpperInvariant())))
            [..8]
            .ToLowerInvariant();
        return AppendSuffix(baseUrl, hash);
    }

    // Burada çok düşük olasılıklı ikinci URL çakışması için benzersiz bir yedek URL oluşturuyorum.
    private static string CreateRandomCollisionUrl(string baseUrl)
    {
        return AppendSuffix(baseUrl, Guid.NewGuid().ToString("N")[..12]);
    }

    // Burada etiket URL'sine uzunluk sınırını aşmadan güvenli bir ek bağlıyorum.
    private static string AppendSuffix(string baseUrl, string suffix)
    {
        var maximumBaseLength = MaximumTagUrlLength - suffix.Length - 1;
        var trimmedBaseUrl = baseUrl.Length > maximumBaseLength
            ? baseUrl[..maximumBaseLength].TrimEnd('-')
            : baseUrl;
        return $"{trimmedBaseUrl}-{suffix}";
    }

    // Burada çözümleme sırasında kullanılacak temiz etiket adı ve URL adaylarını taşıyorum.
    private sealed record PreparedTag(string Name, string BaseUrl, string CollisionUrl);
}
