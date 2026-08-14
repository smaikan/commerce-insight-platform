namespace ECommerce.Persistence.Search;

// Burada her ürünün birleşik ve normalize edilmiş arama metnini tek satırda tutuyorum.
public sealed class ProductSearchDocument
{
    public long ProductId { get; set; }
    public string TitleNormalized { get; set; } = string.Empty;
    public string BrandNormalized { get; set; } = string.Empty;
    public string TypeNormalized { get; set; } = string.Empty;
    public string CollectionNamesNormalized { get; set; } = string.Empty;
    public string TagNamesNormalized { get; set; } = string.Empty;
    public string MainSkuNormalized { get; set; } = string.Empty;
    public string SearchTextNormalized { get; set; } = string.Empty;
}
