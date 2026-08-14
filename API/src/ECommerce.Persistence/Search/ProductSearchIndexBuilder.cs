using ECommerce.Application.Products.Services;

namespace ECommerce.Persistence.Search;

// Burada test, bakım ve doğrulama araçları için SQL migration'ıyla aynı arama dokümanını üretiyorum.
public static class ProductSearchIndexBuilder
{
    // Burada ürün ve sınıflandırma metinlerini tek normalize arama dokümanında birleştiriyorum.
    public static ProductSearchDocument CreateDocument(
        long productId,
        string title,
        string mainSku,
        string? brandName = null,
        string? typeName = null,
        IEnumerable<string>? collectionNames = null,
        IEnumerable<string>? tagNames = null)
    {
        var collections = ProductSearchTextNormalizer.Normalize(string.Join(' ', collectionNames ?? []));
        var tags = ProductSearchTextNormalizer.Normalize(string.Join(' ', tagNames ?? []));
        var document = new ProductSearchDocument
        {
            ProductId = productId,
            TitleNormalized = Limit(ProductSearchTextNormalizer.Normalize(title), 250),
            BrandNormalized = Limit(ProductSearchTextNormalizer.Normalize(brandName), 150),
            TypeNormalized = Limit(ProductSearchTextNormalizer.Normalize(typeName), 150),
            CollectionNamesNormalized = Limit(collections, 2000),
            TagNamesNormalized = Limit(tags, 2000),
            MainSkuNormalized = Limit(ProductSearchTextNormalizer.Normalize(mainSku), 100)
        };
        document.SearchTextNormalized = Limit(ProductSearchTextNormalizer.Normalize(string.Join(' ', new[]
        {
            title,
            brandName,
            typeName,
            collections,
            tags,
            mainSku
        }.Where(value => !string.IsNullOrWhiteSpace(value)))), 4000);
        return document;
    }

    // Burada dokümandaki bütün kelimelerden benzersiz iki ve üç karakterli arama gramlarını üretiyorum.
    public static IReadOnlyList<ProductSearchGram> CreateGrams(ProductSearchDocument document) =>
        document.SearchTextNormalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(CreateWordGrams)
            .Distinct(StringComparer.Ordinal)
            .Select(gram => new ProductSearchGram { Gram = gram, ProductId = document.ProductId })
            .ToArray();

    // Burada tek kelimenin bütün iki ve üç karakterli parçalarını oluşturuyorum.
    private static IEnumerable<string> CreateWordGrams(string word)
    {
        for (var index = 0; index < word.Length - 1; index++)
        {
            yield return word.Substring(index, 2);
            if (index < word.Length - 2)
            {
                yield return word.Substring(index, 3);
            }
        }
    }

    // Burada SQL kolon sınırını aşan normalize metni güvenli uzunlukta kesiyorum.
    private static string Limit(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];
}
