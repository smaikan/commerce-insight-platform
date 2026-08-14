namespace ECommerce.Persistence.Search;

// Burada iki ve üç karakterli aday gramını ürün kimliğiyle indekslenebilir biçimde tutuyorum.
public sealed class ProductSearchGram
{
    public string Gram { get; set; } = string.Empty;
    public long ProductId { get; set; }
}
