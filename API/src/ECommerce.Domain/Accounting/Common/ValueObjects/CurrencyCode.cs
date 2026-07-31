using ECommerce.Domain.Common;

namespace ECommerce.Domain.Accounting.Common.ValueObjects;

public sealed record CurrencyCode
{
    public const int RequiredLength = 3;

    public string Value { get; }

    // Burada para birimi kodunu üç ASCII harften oluşan büyük harfli kanonik biçime getiriyorum.
    public CurrencyCode(string value)
    {
        Value = Normalize(value);
    }

    // Burada para birimi kodunun kanonik metin değerini dış kullanıma veriyorum.
    public override string ToString()
    {
        return Value;
    }

    // Burada para birimi kodunu boşluk, uzunluk ve ASCII harf kurallarına göre doğruluyorum.
    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Currency code cannot be empty.");
        }

        var normalizedValue = value.Trim().ToUpperInvariant();
        if (normalizedValue.Length != RequiredLength)
        {
            throw new DomainException($"Currency code must contain exactly {RequiredLength} characters.");
        }

        if (normalizedValue.Any(character => character is < 'A' or > 'Z'))
        {
            throw new DomainException("Currency code can contain only ASCII letters.");
        }

        return normalizedValue;
    }
}
