using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class VariantOptionValue : AuditableEntity
{
    public Guid VariantOptionNameId { get; private set; }
    public VariantOptionName VariantOptionName { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public ICollection<ProductVariant> ProductVariants { get; private set; } = new List<ProductVariant>();

    // Burada EF Core'un varyant değerini veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private VariantOptionValue()
    {
    }

    // Burada bir varyant adına bağlı merkezi değeri doğrulayıp kayda hazırlıyorum.
    public VariantOptionValue(VariantOptionName variantOptionName, string value)
    {
        VariantOptionName = variantOptionName ?? throw new DomainException("Variant option name is required.");
        VariantOptionNameId = variantOptionName.Id;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Variant option value cannot be empty.");
        }

        Value = value.Trim();
    }
}
