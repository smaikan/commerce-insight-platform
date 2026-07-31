using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProductVariantOptionValue : BaseEntity
{
    public Guid ProductVariantId { get; private set; }
    public ProductVariant ProductVariant { get; private set; } = null!;
    public Guid VariantOptionNameId { get; private set; }
    public Guid VariantOptionValueId { get; private set; }
    public VariantOptionValue VariantOptionValue { get; private set; } = null!;
    public int DisplayOrder { get; private set; }

    // Burada EF Core için boş kurucuyu tutuyorum.
    private ProductVariantOptionValue() { }

    // Burada varyantın tek ad-değer seçimini sırasıyla bağlıyorum.
    public ProductVariantOptionValue(ProductVariant variant, VariantOptionName name, VariantOptionValue value, int displayOrder)
    {
        ProductVariant = variant ?? throw new DomainException("Product variant is required.");
        ProductVariantId = variant.Id;
        VariantOptionNameId = name?.Id ?? throw new DomainException("Variant option name is required.");
        VariantOptionValue = value ?? throw new DomainException("Variant option value is required.");
        VariantOptionValueId = value.Id;
        if (value.VariantOptionNameId != VariantOptionNameId || displayOrder < 0) throw new DomainException("Variant option selection is invalid.");
        DisplayOrder = displayOrder;
    }
}
