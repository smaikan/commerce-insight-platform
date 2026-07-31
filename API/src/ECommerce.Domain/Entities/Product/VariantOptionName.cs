using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class VariantOptionName : AuditableEntity
{
    public string Name { get; private set; } = null!;
    public ICollection<VariantOptionValue> Values { get; private set; } = new List<VariantOptionValue>();
    public ICollection<ProductVariant> ProductVariants { get; private set; } = new List<ProductVariant>();

    // Burada EF Core'un varyant adını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private VariantOptionName()
    {
    }

    // Burada merkezi varyant adını doğrulayıp kayda hazırlıyorum.
    public VariantOptionName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Variant option name cannot be empty.");
        }

        Name = name.Trim();
    }
}
