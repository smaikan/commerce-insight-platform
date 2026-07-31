using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class ProductVariant : AuditableEntity
{
    private readonly List<StockMovement> _stockMovements = [];
    private readonly List<ProductVariantOptionValue> _optionValues = [];

    public long ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public Guid? VariantOptionNameId { get; private set; }
    public VariantOptionName? VariantOptionName { get; private set; }
    public Guid? VariantOptionValueId { get; private set; }
    public VariantOptionValue? VariantOptionValue { get; private set; }
    public string Sku { get; private set; } = null!;
    public string? Barcode { get; private set; }
    public string? Material { get; private set; }
    // Burada müşteriye gösterilen vergi dahil satış fiyatını saklıyorum.
    public decimal Price { get; private set; }
    // Burada muhasebe ve sipariş hesapları için vergi hariç satış fiyatını saklıyorum.
    public decimal NetPrice { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public int Stock { get; private set; }
    public long AddToCartCount { get; private set; }
    public long PurchaseCount { get; private set; }
    public bool IsActive { get; private set; }
    public Guid ConcurrencyToken { get; private set; }

    public ICollection<ProductVariantDailyMetric> DailyMetrics { get; private set; } = new List<ProductVariantDailyMetric>();
    public IReadOnlyCollection<StockMovement> StockMovements => _stockMovements.AsReadOnly();
    public IReadOnlyCollection<ProductVariantOptionValue> OptionValues => _optionValues.AsReadOnly();

    // Burada EF Core'un varyantı veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ProductVariant()
    {
    }

    // Burada ürün kimliğiyle yeni bir satılabilir varyant ve varsa açılış stok hareketini oluşturuyorum.
    public ProductVariant(
        long productId,
        string name,
        string sku,
        decimal price,
        int stock,
        decimal? compareAtPrice = null,
        string? barcode = null,
        string? material = null,
        bool isActive = true,
        decimal? netPrice = null,
        string? value = null)
    {
        if (productId <= 0)
        {
            throw new DomainException("Product id is required.");
        }

        ProductId = productId;
        SetName(name);
        SetValue(value ?? name);
        SetSku(sku);
        SetPrice(price, compareAtPrice, netPrice ?? price);
        Barcode = barcode?.Trim();
        Material = material?.Trim();
        IsActive = isActive;
        ConcurrencyToken = Guid.NewGuid();
        InitializeStock(stock);
    }

    // Burada ürün nesnesine bağlı yeni bir satılabilir varyant ve varsa açılış stok hareketini oluşturuyorum.
    public ProductVariant(
        Product product,
        string name,
        string sku,
        decimal price,
        int stock,
        decimal? compareAtPrice = null,
        string? barcode = null,
        string? material = null,
        bool isActive = true,
        decimal? netPrice = null,
        string? value = null)
        : this(1, name, sku, price, stock, compareAtPrice, barcode, material, isActive, netPrice, value)
    {
        Product = product ?? throw new DomainException("Product cannot be empty.");
        ProductId = product.Id;
    }

    // Burada stok değişimini hareket kaydıyla birlikte tek aggregate işlemi olarak uyguluyorum.
    public StockMovement ApplyStockMovement(
        int quantityDelta,
        StockMovementType type,
        string? reason = null,
        Guid? orderId = null,
        Guid? returnRequestId = null)
    {
        return ApplyStockMovementCore(
            quantityDelta,
            type,
            reason,
            orderId,
            returnRequestId,
            markAsChanged: true);
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

    // Burada varyantın vergi dahil ve vergi hariç fiyatlarını birlikte güncelliyorum.
    public void UpdatePrice(decimal price, decimal? compareAtPrice, decimal? netPrice = null)
    {
        SetPrice(price, compareAtPrice, netPrice ?? price);
        MarkAsChanged();
    }

    // Burada ürünün seçili vergi oranına göre saklanan vergi hariç fiyatı yeniden hesaplıyorum.
    public void RecalculateNetPrice(TaxRate? taxRate)
    {
        NetPrice = taxRate?.CalculateNetPrice(Price) ?? Price;
        MarkAsChanged();
    }

    // Burada varyantın tanımlayıcı bilgilerini güncelliyorum.
    public void UpdateDetails(
        string name,
        string value,
        string sku,
        string? barcode,
        string? material)
    {
        SetName(name);
        SetValue(value);
        SetSku(sku);
        Barcode = barcode?.Trim();
        Material = material?.Trim();
        MarkAsChanged();
    }

    // Burada eski çağrıların mevcut değeri koruyarak varyant detayını güncellemesini sağlıyorum.
    public void UpdateDetails(
        string name,
        string sku,
        string? barcode,
        string? material)
    {
        UpdateDetails(name, Value, sku, barcode, material);
    }

    // Burada varyantın merkezi ad ve değer kayıtlarıyla metin alanlarının tutarlılığını kuruyorum.
    public void AssignVariantOption(
        VariantOptionName variantOptionName,
        VariantOptionValue variantOptionValue)
    {
        if (variantOptionName is null || variantOptionValue is null ||
            variantOptionValue.VariantOptionNameId != variantOptionName.Id ||
            variantOptionValue.VariantOptionNameId != variantOptionName.Id)
        {
            throw new DomainException("Variant option name and value must match the product variant.");
        }

        VariantOptionName = variantOptionName;
        VariantOptionNameId = variantOptionName.Id;
        VariantOptionValue = variantOptionValue;
        VariantOptionValueId = variantOptionValue.Id;
        MarkAsChanged();
    }

    // Burada ayrıştırılmış en fazla üç ad-değer seçimini varyanta bağlıyorum.
    public void ReplaceOptionValues(IReadOnlyList<VariantOptionSelection> options)
    {
        if (options.Count is < 1 or > 3 || options.Select(item => item.Name.Id).Distinct().Count() != options.Count)
            throw new DomainException("A variant must contain between one and three unique option names.");
        _optionValues.Clear();
        for (var index = 0; index < options.Count; index++) _optionValues.Add(new ProductVariantOptionValue(this, options[index].Name, options[index].Value, index));
        AssignVariantOption(options[0].Name, options[0].Value);
    }

    // Burada ilk stok değerini açılış hareketi üzerinden oluşturuyorum.
    private void InitializeStock(int stock)
    {
        if (stock < 0)
        {
            throw new DomainException("Stock cannot be negative.");
        }

        if (stock == 0)
        {
            return;
        }

        ApplyStockMovementCore(
            stock,
            StockMovementType.OpeningBalance,
            reason: null,
            orderId: null,
            returnRequestId: null,
            markAsChanged: false);
    }

    // Burada hareketi aggregate'e ekleyip güncel stok bakiyesini aynı anda değiştiriyorum.
    private StockMovement ApplyStockMovementCore(
        int quantityDelta,
        StockMovementType type,
        string? reason,
        Guid? orderId,
        Guid? returnRequestId,
        bool markAsChanged)
    {
        var movement = new StockMovement(
            this,
            quantityDelta,
            type,
            reason,
            orderId,
            returnRequestId);

        _stockMovements.Add(movement);
        Stock = movement.StockAfterMovement;

        if (markAsChanged)
        {
            MarkAsChanged();
        }

        return movement;
    }

    // Burada vergi dahil, vergi hariç ve karşılaştırma fiyatlarının tutarlılığını doğruluyorum.
    private void SetPrice(decimal price, decimal? compareAtPrice, decimal netPrice)
    {
        if (price <= 0)
        {
            throw new DomainException("Price must be greater than zero.");
        }

        if (compareAtPrice.HasValue && compareAtPrice.Value < price)
        {
            throw new DomainException("Compare-at price cannot be lower than price.");
        }

        if (netPrice <= 0m || netPrice > price)
        {
            throw new DomainException("Net price must be greater than zero and cannot exceed tax-inclusive price.");
        }

        Price = price;
        NetPrice = netPrice;
        CompareAtPrice = compareAtPrice;
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

    // Burada varyant değerini doğrulayıp temizliyorum.
    private void SetValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Variant value cannot be empty.");
        }

        Value = value.Trim();
    }

    // Burada varyant değişikliğini concurrency ve audit alanlarına yansıtıyorum.
    private void MarkAsChanged()
    {
        ConcurrencyToken = Guid.NewGuid();
        MarkAsUpdated();
    }
}
