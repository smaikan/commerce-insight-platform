using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductVariant : AuditableEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public string? Barcode { get; private set; }
    public string? Color { get; private set; }
    public string? Size { get; private set; }
    public string? Material { get; private set; }
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }
    public int Stock { get; private set; }
    public int AddToCartCount { get; private set; }
    public int PurchaseCount { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<ProductVariantDailyMetric> DailyMetrics { get; private set; } = new List<ProductVariantDailyMetric>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; private set; } = new List<InventoryTransaction>();

    private ProductVariant()
    {
    }

    public ProductVariant(
        Guid productId,
        string sku,
        decimal price,
        int stock,
        decimal? compareAtPrice = null,
        string? barcode = null,
        string? color = null,
        string? size = null,
        string? material = null,
        bool isActive = true)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Product id is required.");
        }

        ProductId = productId;
        SetSku(sku);
        SetPrice(price, compareAtPrice);
        SetStock(stock);
        Barcode = barcode?.Trim();
        Color = color?.Trim();
        Size = size?.Trim();
        Material = material?.Trim();
        IsActive = isActive;
    }

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
        MarkAsUpdated();
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        Stock += quantity;
        MarkAsUpdated();
    }

    public void IncreaseAddToCartCount(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        AddToCartCount += quantity;
        MarkAsUpdated();
    }

    public void IncreasePurchaseCount(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be greater than zero.");
        }

        PurchaseCount += quantity;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void UpdatePrice(decimal price, decimal? compareAtPrice)
    {
        SetPrice(price, compareAtPrice);
        MarkAsUpdated();
    }

    public void UpdateDetails(
        string sku,
        string? barcode,
        string? color,
        string? size,
        string? material)
    {
        SetSku(sku);
        Barcode = barcode?.Trim();
        Color = color?.Trim();
        Size = size?.Trim();
        Material = material?.Trim();
        MarkAsUpdated();
    }

    public void UpdateStock(int stock)
    {
        SetStock(stock);
        MarkAsUpdated();
    }

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

    private void SetStock(int stock)
    {
        if (stock < 0)
        {
            throw new DomainException("Stock cannot be negative.");
        }

        Stock = stock;
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("Variant SKU cannot be empty.");
        }

        Sku = sku.Trim();
    }
}
