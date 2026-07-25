namespace ECommerce.Application.Common.Services;

public static class ProductTagRules
{
    // Burada tek ürün isteğinde kabul edilen en fazla etiket sayısını tanımlıyorum.
    public const int MaximumTagsPerProduct = 20;

    // Burada toplu ürün isteğinde otomatik çözümlenecek en fazla benzersiz etiket sayısını tanımlıyorum.
    public const int MaximumUniqueTagsPerBulkRequest = 1000;

    // Burada etiket adının veritabanıyla uyumlu en fazla uzunluğunu tanımlıyorum.
    public const int MaximumTagNameLength = 150;
}

public interface IProductTagResolver
{
    // Burada ürün altında girilen etiket adlarını mevcut veya yeni etiket kimliklerine çözümlüyorum.
    Task<ProductTagResolution> ResolveAsync(
        IEnumerable<string>? tagNames,
        CancellationToken cancellationToken = default);
}

public sealed class ProductTagResolution
{
    private readonly IReadOnlyDictionary<string, Guid> _tagIdsByName;

    public static ProductTagResolution Empty { get; } =
        new(new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));

    // Burada çözümlenen etiket adlarıyla kimlikleri arasındaki eşlemeyi saklıyorum.
    public ProductTagResolution(IReadOnlyDictionary<string, Guid> tagIdsByName)
    {
        ArgumentNullException.ThrowIfNull(tagIdsByName);
        _tagIdsByName = tagIdsByName;
    }

    // Burada tek ürünün etiket adlarını tekrar etmeyen etiket kimliklerine dönüştürüyorum.
    public IReadOnlyList<Guid> GetIds(IEnumerable<string>? tagNames)
    {
        if (tagNames is null)
        {
            return [];
        }

        return tagNames
            .Where(tagName => !string.IsNullOrWhiteSpace(tagName))
            .Select(tagName => _tagIdsByName[tagName.Trim()])
            .Distinct()
            .ToList();
    }
}
