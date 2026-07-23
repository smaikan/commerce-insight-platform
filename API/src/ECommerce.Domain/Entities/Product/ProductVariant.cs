using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductVariant : AuditableEntity
{
    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public string? Barcode { get; private set; }
    public string? Material { get; private set; }
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public int Stock { get; private set; }
    public long AddToCartCount { get; private set; }
    public long PurchaseCount { get; private set; }
    public bool IsActive { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public ICollection<ProductVariantDailyMetric> DailyMetrics { get; private set; } = new List<ProductVariantDailyMetric>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; private set; } = new List<InventoryTransaction>();

    // Burada EF Core'un varyantı veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ProductVariant()
    {
    }

    // Burada ürün kimliğiyle yeni bir satılabilir varyant oluşturuyorum.
    public ProductVariant(
        long productId,
        string name,
        string sku,
        decimal price,
        int stock,
        decimal? compareAtPrice = null,
        string? barcode = null,
        string? material = null,
        bool isActive = true)
    {
        if (productId <= 0)
        {
            throw new DomainException("Product id is required.");
        }

        ProductId = productId;
        SetName(name);
        SetSku(sku);
        SetPrice(price, compareAtPrice);
        SetStock(stock);
        Barcode = barcode?.Trim();
        Material = material?.Trim();
        IsActive = isActive;
        ConcurrencyToken = Guid.NewGuid();
    }

    // Burada ürün nesnesine bağlı yeni bir satılabilir varyant oluşturuyorum.
    public ProductVariant(
        Product product,
        string name,
        string sku,
        decimal price,
        int stock,
        decimal? compareAtPrice = null,
        string? barcode = null,
        string? material = null,
        bool isActive = true)
        : this(1, name, sku, price, stock, compareAtPrice, barcode, material, isActive)
    {
        Product = product ?? throw new DomainException("Product cannot be empty.");
        ProductId = product.Id;
    }

    // Burada stok miktarını negatif olmayacak şekilde azaltıyorum.
    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (Stock - quantity < 0)
        {
            throw new DomainException("Stock cannot be negative.");
        }

        Stock -= quantity;
        MarkAsChanged();
    }

    // Burada stok miktarını sayı taşmasına izin vermeden artırıyorum.
    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        if (quantity > int.MaxValue - Stock)
        {
            throw new DomainException("Stock cannot exceed the maximum supported value.");
        }

        Stock += quantity;
        MarkAsChanged();
    }

    // Burada sepete ekleme sayacını güvenli uzunlukta artırıyorum.
    public void IncreaseAddToCartCount(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        AddToCartCount += quantity;
        MarkAsChanged();
    }

    // Burada satın alma sayacını güvenli uzunlukta artırıyorum.
    public void IncreasePurchaseCount(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        PurchaseCount += quantity;
        MarkAsChanged();
    }

    // Burada varyantı satışa açıyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsChanged();
    }

    // Burada varyantı satışa kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsChanged();
    }

    // Burada varyant fiyatlarını güncelliyorum.
    public void UpdatePrice(decimal price, decimal? compareAtPrice)
    {
        SetPrice(price, compareAtPrice);
        MarkAsChanged();
    }

    // Burada varyantın tanımlayıcı bilgilerini güncelliyorum.
    public void UpdateDetails(
        string name,
        string sku,
        string? barcode,
        string? material)
    {
        SetName(name);
        SetSku(sku);
        Barcode = barcode?.Trim();
        Material = material?.Trim();
        MarkAsChanged();
    }

    // Burada stok değerini doğrudan ve doğrulanmış şekilde güncelliyorum.
    public void UpdateStock(int stock)
    {
        SetStock(stock);
        MarkAsChanged();
    }

    // Burada fiyat ve karşılaştırma fiyatı tutarlılığını doğruluyorum.
    private void SetPrice(decimal price, decimal? compareAtPrice)
    {
        if (price <= 0)
        {
            throw new DomainException("Price must be greater than zero.");
        }

        if (compareAtPrice.HasValue && compareAtPrice.Value < price)
        {
            throw new DomainException("Compare-at price cannot be lower than price.");
        }

        Price = price;
        CompareAtPrice = compareAtPrice;
    }

    // Burada stok değerinin negatif olmadığını doğruluyorum.
    private void SetStock(int stock)
    {
        if (stock < 0)
        {
            throw new DomainException("Stock cannot be negative.");
        }

        Stock = stock;
    }

    // Burada SKU değerini doğrulayıp temizliyorum.
    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("Variant SKU cannot be empty.");
        }

        Sku = sku.Trim();
    }

    // Burada varyant adını doğrulayıp temizliyorum.
    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Variant name cannot be empty.");
        }

        Name = name.Trim();
    }

    // Burada varyant değişikliğini concurrency ve audit alanlarına yansıtıyorum.
    private void MarkAsChanged()
    {
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }
}
