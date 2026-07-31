using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ShippingMethod : AuditableEntity
{
    public const int MaximumNameLength = 150;

    public string Name { get; private set; } = null!;
    public decimal FixedFee { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }

    // Burada EF Core'un kargo yöntemi kaydını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private ShippingMethod()
    {
    }

    // Burada sabit ücretli kargo yöntemini gösterim ayarlarıyla oluşturuyorum.
    public ShippingMethod(string name, decimal fixedFee, bool isActive = true, int displayOrder = 0)
    {
        SetName(name);
        SetFixedFee(fixedFee);
        SetDisplayOrder(displayOrder);
        IsActive = isActive;
    }

    // Burada kargo yönteminin müşteriye gösterilecek adını değiştiriyorum.
    public void Rename(string name)
    {
        SetName(name);
        MarkAsUpdated();
    }

    // Burada müşterinin toplamına eklenecek sabit kargo ücretini güncelliyorum.
    public void ChangeFixedFee(decimal fixedFee)
    {
        SetFixedFee(fixedFee);
        MarkAsUpdated();
    }

    // Burada kargo yönteminin listeleme sırasını güncelliyorum.
    public void ChangeDisplayOrder(int displayOrder)
    {
        SetDisplayOrder(displayOrder);
        MarkAsUpdated();
    }

    // Burada kargo yöntemini yeni checkout seçimlerine açıyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    // Burada kargo yöntemini geçmiş siparişleri etkilemeden yeni checkout seçimlerine kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada kargo yöntemi adını boşluk ve uzunluk kurallarına göre saklıyorum.
    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Shipping method name cannot be empty.");
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaximumNameLength)
        {
            throw new DomainException($"Shipping method name cannot exceed {MaximumNameLength} characters.");
        }

        Name = normalizedName;
    }

    // Burada sabit kargo ücretinin negatif olmadığını doğruluyorum.
    private void SetFixedFee(decimal fixedFee)
    {
        if (fixedFee < 0m)
        {
            throw new DomainException("Shipping method fixed fee cannot be negative.");
        }

        if (fixedFee > OrderItem.MaximumSupportedAmount)
        {
            throw new DomainException("Shipping method fixed fee exceeds the supported monetary limit.");
        }

        if (decimal.Round(fixedFee, OrderItem.SupportedPriceScale) != fixedFee)
        {
            throw new DomainException($"Shipping method fixed fee cannot have more than {OrderItem.SupportedPriceScale} decimal places.");
        }

        FixedFee = fixedFee;
    }

    // Burada yönetim sıralamasının negatif olmadığını doğruluyorum.
    private void SetDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
        {
            throw new DomainException("Shipping method display order cannot be negative.");
        }

        DisplayOrder = displayOrder;
    }
}
