using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Address : AuditableEntity
{
    public const int MaximumTitleLength = 100;
    public const int MaximumFirstNameLength = 100;
    public const int MaximumLastNameLength = 100;
    public const int MaximumPhoneNumberLength = 30;
    public const int MaximumCityLength = 100;
    public const int MaximumDistrictLength = 100;
    public const int MaximumFullAddressLength = 500;
    public const int MaximumPostalCodeLength = 20;

    public long UserId { get; private set; }
    public AddressType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string District { get; private set; } = null!;
    public string FullAddress { get; private set; } = null!;
    public string? PostalCode { get; private set; }
    public bool IsDefault { get; private set; }

    // Burada EF Core'un adres kaydını veritabanından oluşturabilmesi için boş kurucuyu tutuyorum.
    private Address()
    {
    }

    // Burada kullanıcının teslimat veya fatura adresini alan sınırlarıyla birlikte oluşturuyorum.
    public Address(
        long userId,
        AddressType type,
        string title,
        string firstName,
        string lastName,
        string phoneNumber,
        string city,
        string district,
        string fullAddress,
        string? postalCode = null,
        bool isDefault = false)
    {
        if (userId <= 0)
        {
            throw new DomainException("User id is required.");
        }

        UserId = userId;
        ApplyDetails(type, title, firstName, lastName, phoneNumber, city, district, fullAddress, postalCode);
        IsDefault = isDefault;
    }

    // Burada adres sahibini değiştirmeden düzenlenebilir iletişim ve konum bilgilerini güncelliyorum.
    public void Update(
        AddressType type,
        string title,
        string firstName,
        string lastName,
        string phoneNumber,
        string city,
        string district,
        string fullAddress,
        string? postalCode = null)
    {
        ApplyDetails(type, title, firstName, lastName, phoneNumber, city, district, fullAddress, postalCode);
        MarkAsUpdated();
    }

    // Burada adresi kendi türündeki varsayılan seçim için işaretliyorum.
    public void SetAsDefault()
    {
        IsDefault = true;
        MarkAsUpdated();
    }

    // Burada adresin varsayılan olma işaretini güvenle kaldırıyorum.
    public void UnsetDefault()
    {
        IsDefault = false;
        MarkAsUpdated();
    }

    // Burada tüm adres alanlarını domain ve veritabanı uzunluk kurallarına göre hazırlıyorum.
    private void ApplyDetails(
        AddressType type,
        string title,
        string firstName,
        string lastName,
        string phoneNumber,
        string city,
        string district,
        string fullAddress,
        string? postalCode)
    {
        Type = ValidateType(type);
        Title = NormalizeRequired(title, MaximumTitleLength, "Address title");
        FirstName = NormalizeRequired(firstName, MaximumFirstNameLength, "Address first name");
        LastName = NormalizeRequired(lastName, MaximumLastNameLength, "Address last name");
        PhoneNumber = NormalizeRequired(phoneNumber, MaximumPhoneNumberLength, "Address phone number");
        City = NormalizeRequired(city, MaximumCityLength, "Address city");
        District = NormalizeRequired(district, MaximumDistrictLength, "Address district");
        FullAddress = NormalizeRequired(fullAddress, MaximumFullAddressLength, "Full address");
        PostalCode = NormalizeOptional(postalCode, MaximumPostalCodeLength, "Postal code");
    }

    // Burada dışarıdan gelen adres türünün tanımlı enum değeri olduğunu doğruluyorum.
    private static AddressType ValidateType(AddressType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Address type is invalid.");
        }

        return type;
    }

    // Burada zorunlu metin alanlarını boşluk ve uzunluk kurallarına göre normalize ediyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }

    // Burada isteğe bağlı posta kodunu boş değerleri null yaparak sınır içinde saklıyorum.
    private static string? NormalizeOptional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue = value.Trim();
        if (normalizedValue.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalizedValue;
    }
}
