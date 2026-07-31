using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class TaxRate : AuditableEntity
{
    public const int MaximumNameLength = 100;
    public const decimal MinimumRate = 0m;
    public const decimal MaximumRate = 100m;

    public string Name { get; private set; } = null!;
    public decimal Rate { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    // Burada EF Core'un vergi oranı kaydını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private TaxRate()
    {
    }

    // Burada ürünlere atanabilecek vergi oranını temel kurallarıyla oluşturuyorum.
    public TaxRate(string? name, decimal rate, bool isActive = true)
    {
        if (name is not null)
        {
            SetName(name);
        }
        
        SetRate(rate);
        IsActive = isActive;
    }

    // Burada vergi oranının yönetim ekranındaki adını değiştiriyorum.
    public void Rename(string name)
    {
        SetName(name);
        MarkAsUpdated();
    }

    // Burada vergi yüzdesini geçerli sınırlar içinde güncelliyorum.
    public void ChangeRate(decimal rate)
    {
        SetRate(rate);
        MarkAsUpdated();
    }

    // Burada vergi oranını yeni ürün seçimlerine açıyorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    // Burada vergi oranını geçmiş ilişkileri bozmadan yeni seçimlere kapatıyorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada vergi dahil tutardan iki ondalık hassasiyetle vergi hariç tutarı türetiyorum.
    public decimal CalculateNetPrice(decimal taxInclusivePrice)
    {
        if (taxInclusivePrice <= 0m)
        {
            throw new DomainException("Tax-inclusive price must be greater than zero.");
        }

        return decimal.Round(
            taxInclusivePrice / (1m + (Rate / 100m)),
            OrderItem.SupportedPriceScale,
            MidpointRounding.AwayFromZero);
    }

    // Burada vergi oranı adını boşluk ve uzunluk kurallarına göre saklıyorum.
    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tax rate name cannot be empty.");
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaximumNameLength)
        {
            throw new DomainException($"Tax rate name cannot exceed {MaximumNameLength} characters.");
        }

        Name = normalizedName;
    }

    // Burada vergi yüzdesinin sıfır ile yüz arasında kaldığını doğruluyorum.
    private void SetRate(decimal rate)
    {
        if (rate < MinimumRate || rate > MaximumRate)
        {
            throw new DomainException($"Tax rate must be between {MinimumRate} and {MaximumRate}.");
        }

        if (decimal.Round(rate, OrderItem.SupportedPriceScale) != rate)
        {
            throw new DomainException($"Tax rate cannot have more than {OrderItem.SupportedPriceScale} decimal places.");
        }

        Rate = rate;
    }
}
