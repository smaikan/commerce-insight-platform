using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Coupon : AuditableEntity
{
    public const int MaximumCodeLength = 50;
    public const int MaximumDescriptionLength = 1000;
    public const int SupportedMoneyScale = 2;
    public const string CodePattern = "^[A-Za-z0-9_-]+$";
    public const decimal MaximumSupportedAmount = 9999999999999999.99m;

    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public CouponDiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public int? UsageLimit { get; private set; }
    public int UsedCount { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }

    // Burada EF Core'un kupon kaydÄ±nÄ± veritabanÄ±ndan oluÅŸturabilmesi iÃ§in boÅŸ kurucuyu tutuyorum.
    private Coupon()
    {
    }

    // Burada yeni kuponun indirim, kullanÄ±m limiti ve geÃ§erlilik kurallarÄ±nÄ± koruyarak oluÅŸturuyorum.
    public Coupon(
        string code,
        CouponDiscountType discountType,
        decimal discountValue,
        string? description = null,
        decimal? minimumOrderAmount = null,
        int? usageLimit = null,
        DateTime? startsAt = null,
        DateTime? expiresAt = null,
        bool isActive = true)
    {
        ApplyDetails(
            code,
            discountType,
            discountValue,
            description,
            minimumOrderAmount,
            usageLimit,
            startsAt,
            expiresAt);
        IsActive = isActive;
    }

    // Burada kullanÄ±lmÄ±ÅŸ sayaÃ§ deÄŸerini koruyarak kuponun yÃ¶netilebilir alanlarÄ±nÄ± gÃ¼ncelliyorum.
    public void Update(
        string code,
        CouponDiscountType discountType,
        decimal discountValue,
        string? description,
        decimal? minimumOrderAmount,
        int? usageLimit,
        DateTime? startsAt,
        DateTime? expiresAt)
    {
        ApplyDetails(
            code,
            discountType,
            discountValue,
            description,
            minimumOrderAmount,
            usageLimit,
            startsAt,
            expiresAt);
        MarkAsUpdated();
    }

    // Burada kuponun belirtilen UTC anda aktif, sÃ¼resi geÃ§memiÅŸ ve limitinin dolmamÄ±ÅŸ olduÄŸunu sorguluyorum.
    public bool IsAvailableAt(DateTime utcNow)
    {
        EnsureUtc(utcNow, "Coupon availability time");

        return IsActive &&
               (!StartsAt.HasValue || StartsAt.Value <= utcNow) &&
               (!ExpiresAt.HasValue || ExpiresAt.Value >= utcNow) &&
               (!UsageLimit.HasValue || UsedCount < UsageLimit.Value);
    }

    // Burada geÃ§erli kuponun sipariÅŸ tutarÄ±na uygulayacaÄŸÄ± iki ondalÄ±klÄ± indirim tutarÄ±nÄ± hesaplÄ±yorum.
    public decimal CalculateDiscount(decimal orderAmount, DateTime utcNow)
    {
        ValidateOrderAmount(orderAmount);
        EnsureCanBeUsedAt(utcNow);

        if (MinimumOrderAmount.HasValue && orderAmount < MinimumOrderAmount.Value)
        {
            throw new DomainException("Order amount does not meet the coupon minimum amount.");
        }

        var discount = DiscountType switch
        {
            CouponDiscountType.Percentage => decimal.Round(
                orderAmount * DiscountValue / 100m,
                SupportedMoneyScale,
                MidpointRounding.AwayFromZero),
            CouponDiscountType.FixedAmount => DiscountValue,
            _ => throw new DomainException("Coupon discount type is invalid.")
        };

        return Math.Min(discount, orderAmount);
    }

    // Burada varsayÄ±lan UTC zamanla kupon kullanÄ±m sayacÄ±nÄ± bir arttÄ±rÄ±yorum.
    public void IncreaseUsedCount()
    {
        IncreaseUsedCount(DateTime.UtcNow);
    }

    // Burada sadece kullanÄ±labilir kuponun kullanÄ±m sayacÄ±nÄ± taÅŸmaya izin vermeden arttÄ±rÄ±yorum.
    public void IncreaseUsedCount(DateTime utcNow)
    {
        EnsureCanBeUsedAt(utcNow);

        if (UsedCount == int.MaxValue)
        {
            throw new DomainException("Coupon usage count exceeds the supported limit.");
        }

        UsedCount++;
        MarkAsUpdated();
    }

    // Burada geri alÄ±nan kullanÄ±m iÃ§in kupon sayacÄ±nÄ± negatif olmayacak biÃ§imde azaltÄ±yorum.
    public void DecreaseUsedCount()
    {
        if (UsedCount <= 0)
        {
            throw new DomainException("Used count cannot become negative.");
        }

        UsedCount--;
        MarkAsUpdated();
    }

    // Burada kuponu yeni kullanÄ±mlara aÃ§Ä±yorum.
    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    // Burada kuponu yeni kullanÄ±mlara kapatÄ±yorum.
    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    // Burada kuponun deÄŸiÅŸtirilebilir alanlarÄ±nÄ± birlikte doÄŸrulayÄ±p aggregate'a uyguluyorum.
    private void ApplyDetails(
        string code,
        CouponDiscountType discountType,
        decimal discountValue,
        string? description,
        decimal? minimumOrderAmount,
        int? usageLimit,
        DateTime? startsAt,
        DateTime? expiresAt)
    {
        var normalizedCode = NormalizeCode(code);
        ValidateDiscount(discountType, discountValue);
        ValidateMinimumOrderAmount(minimumOrderAmount);
        ValidateUsageLimit(usageLimit);
        ValidateDates(startsAt, expiresAt);

        Code = normalizedCode;
        DiscountType = discountType;
        DiscountValue = discountValue;
        Description = NormalizeDescription(description);
        MinimumOrderAmount = minimumOrderAmount;
        UsageLimit = usageLimit;
        StartsAt = startsAt;
        ExpiresAt = expiresAt;
    }

    // Burada kuponun belirtilen anda kullanÄ±ma uygun olmamasÄ±nÄ± aÃ§Ä±k bir domain hatasÄ±na dÃ¶nÃ¼ÅŸtÃ¼rÃ¼yorum.
    private void EnsureCanBeUsedAt(DateTime utcNow)
    {
        EnsureUtc(utcNow, "Coupon availability time");

        if (!IsActive)
        {
            throw new DomainException("Coupon is inactive.");
        }

        if (StartsAt.HasValue && StartsAt.Value > utcNow)
        {
            throw new DomainException("Coupon is not active yet.");
        }

        if (ExpiresAt.HasValue && ExpiresAt.Value < utcNow)
        {
            throw new DomainException("Coupon has expired.");
        }

        if (UsageLimit.HasValue && UsedCount >= UsageLimit.Value)
        {
            throw new DomainException("Coupon usage limit reached.");
        }
    }

    // Burada sipariÅŸ tutarÄ±nÄ±n pozitif, para hassasiyetine uygun ve desteklenen aralÄ±kta olduÄŸunu denetliyorum.
    private static void ValidateOrderAmount(decimal orderAmount)
    {
        if (orderAmount <= 0m || orderAmount > MaximumSupportedAmount)
        {
            throw new DomainException("Order amount must be within the supported positive monetary range.");
        }

        if (decimal.Round(orderAmount, SupportedMoneyScale) != orderAmount)
        {
            throw new DomainException($"Order amount cannot have more than {SupportedMoneyScale} decimal places.");
        }
    }

    // Burada kupon kodunu bÃ¼yÃ¼k harfli, boÅŸ olmayan ve veritabanÄ± uzunluÄŸuna uygun hale getiriyorum.
    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Coupon code cannot be empty.");
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        if (normalizedCode.Length > MaximumCodeLength)
        {
            throw new DomainException($"Coupon code cannot exceed {MaximumCodeLength} characters.");
        }

        if (!normalizedCode.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '_' or '-'))
        {
            throw new DomainException("Coupon code can contain only letters, numbers, underscores and hyphens.");
        }

        return normalizedCode;
    }

    // Burada indirim tipini ve para hassasiyetine uygun indirim deÄŸerini denetliyorum.
    private static void ValidateDiscount(CouponDiscountType discountType, decimal discountValue)
    {
        if (!Enum.IsDefined(discountType))
        {
            throw new DomainException("Coupon discount type is invalid.");
        }

        if (discountValue <= 0m || discountValue > MaximumSupportedAmount)
        {
            throw new DomainException("Discount value must be within the supported positive monetary range.");
        }

        if (decimal.Round(discountValue, SupportedMoneyScale) != discountValue)
        {
            throw new DomainException($"Discount value cannot have more than {SupportedMoneyScale} decimal places.");
        }

        if (discountType == CouponDiscountType.Percentage && discountValue > 100m)
        {
            throw new DomainException("Percentage discount must be between 0 and 100.");
        }
    }

    // Burada kuponun minimum sipariÅŸ tutarÄ±nÄ±n para sÄ±nÄ±rlarÄ±na uygun olduÄŸunu denetliyorum.
    private static void ValidateMinimumOrderAmount(decimal? minimumOrderAmount)
    {
        if (!minimumOrderAmount.HasValue)
        {
            return;
        }

        if (minimumOrderAmount.Value < 0m || minimumOrderAmount.Value > MaximumSupportedAmount)
        {
            throw new DomainException("Minimum order amount must be within the supported monetary range.");
        }

        if (decimal.Round(minimumOrderAmount.Value, SupportedMoneyScale) != minimumOrderAmount.Value)
        {
            throw new DomainException($"Minimum order amount cannot have more than {SupportedMoneyScale} decimal places.");
        }
    }

    // Burada kullanÄ±m limiti varsa pozitif kaldÄ±ÄŸÄ±nÄ± ve mevcut kullanÄ±mdan dÃ¼ÅŸmediÄŸini denetliyorum.
    private void ValidateUsageLimit(int? usageLimit)
    {
        if (!usageLimit.HasValue)
        {
            return;
        }

        if (usageLimit.Value <= 0)
        {
            throw new DomainException("Usage limit must be greater than zero.");
        }

        if (usageLimit.Value < UsedCount)
        {
            throw new DomainException("Usage limit cannot be lower than the current usage count.");
        }
    }

    // Burada baÅŸlangÄ±Ã§ ve bitiÅŸ zamanlarÄ±nÄ±n UTC ve kronolojik olduÄŸunu denetliyorum.
    private static void ValidateDates(DateTime? startsAt, DateTime? expiresAt)
    {
        if (startsAt.HasValue)
        {
            EnsureUtc(startsAt.Value, "Coupon start time");
        }

        if (expiresAt.HasValue)
        {
            EnsureUtc(expiresAt.Value, "Coupon expiry time");
        }

        if (startsAt.HasValue && expiresAt.HasValue && startsAt.Value > expiresAt.Value)
        {
            throw new DomainException("Coupon start date cannot be later than expiry date.");
        }
    }

    // Burada isteÄŸe baÄŸlÄ± aÃ§Ä±klamayÄ± boÅŸluk ve veritabanÄ± uzunluk kuralÄ±na uygun hale getiriyorum.
    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalizedDescription = description.Trim();
        if (normalizedDescription.Length > MaximumDescriptionLength)
        {
            throw new DomainException($"Coupon description cannot exceed {MaximumDescriptionLength} characters.");
        }

        return normalizedDescription;
    }

    // Burada zaman temelli kurallarÄ±n tutarlÄ± Ã§alÄ±ÅŸmasÄ± iÃ§in UTC zaman zorunluluÄŸunu denetliyorum.
    private static void EnsureUtc(DateTime value, string fieldName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new DomainException($"{fieldName} must be UTC.");
        }
    }
}
