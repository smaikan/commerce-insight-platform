using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

// Burada sipariş anındaki teslimat adresini kullanıcı adresi değişse bile koruyacak snapshot'ı tanımlıyorum.
public sealed class OrderAddressSnapshot : BaseEntity
{
    public const int MaximumTitleLength = 100;
    public const int MaximumNameLength = 100;
    public const int MaximumPhoneNumberLength = 30;
    public const int MaximumCityLength = 100;
    public const int MaximumDistrictLength = 100;
    public const int MaximumNeighborhoodLength = 100;
    public const int MaximumFullAddressLength = 500;
    public const int MaximumPostalCodeLength = 20;

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public Guid? SourceAddressId { get; private set; }
    public AddressType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string District { get; private set; } = null!;
    public string? Neighborhood { get; private set; }
    public string FullAddress { get; private set; } = null!;
    public string? PostalCode { get; private set; }

    // Burada EF Core'un sipariş adresi snapshot'ını oluşturabilmesi için boş kurucuyu tutuyorum.
    private OrderAddressSnapshot()
    {
    }

    // Burada güvenilir kullanıcı adresini siparişe ait değişmez teslimat snapshot'ına dönüştürüyorum.
    internal OrderAddressSnapshot(Order order, Address address, AddressType type)
    {
        if (order is null || order.Id == Guid.Empty || address is null || address.Id == Guid.Empty)
        {
            throw new DomainException("Order and address are required.");
        }

        if (address.Type != type)
        {
            throw new DomainException("The source address type must match the snapshot type.");
        }

        OrderId = order.Id;
        Order = order;
        SourceAddressId = address.Id;
        Type = type;
        Title = CopyRequired(address.Title, MaximumTitleLength, "Address title");
        FirstName = CopyRequired(address.FirstName, MaximumNameLength, "Address first name");
        LastName = CopyRequired(address.LastName, MaximumNameLength, "Address last name");
        PhoneNumber = CopyRequired(address.PhoneNumber, MaximumPhoneNumberLength, "Address phone number");
        City = CopyRequired(address.City, MaximumCityLength, "Address city");
        District = CopyRequired(address.District, MaximumDistrictLength, "Address district");
        Neighborhood = CopyOptional(address.Neighborhood, MaximumNeighborhoodLength, "Address neighborhood");
        FullAddress = CopyRequired(address.FullAddress, MaximumFullAddressLength, "Address full address");
        PostalCode = CopyOptional(address.PostalCode, MaximumPostalCodeLength, "Address postal code");
    }

    // Burada guest veya üyeden gelen güvenilir checkout adresini opsiyonel kaynak kimliğiyle snapshot'a dönüştürüyorum.
    internal OrderAddressSnapshot(
        Order order,
        Guid? sourceAddressId,
        AddressType type,
        string title,
        string firstName,
        string lastName,
        string phoneNumber,
        string city,
        string district,
        string? neighborhood,
        string fullAddress,
        string? postalCode)
    {
        if (order is null || order.Id == Guid.Empty ||
            (sourceAddressId.HasValue && sourceAddressId.Value == Guid.Empty) ||
            !Enum.IsDefined(type))
        {
            throw new DomainException("Order address snapshot values are invalid.");
        }

        OrderId = order.Id;
        Order = order;
        SourceAddressId = sourceAddressId;
        Type = type;
        Title = CopyRequired(title, MaximumTitleLength, "Address title");
        FirstName = CopyRequired(firstName, MaximumNameLength, "Address first name");
        LastName = CopyRequired(lastName, MaximumNameLength, "Address last name");
        PhoneNumber = CopyRequired(phoneNumber, MaximumPhoneNumberLength, "Address phone number");
        City = CopyRequired(city, MaximumCityLength, "Address city");
        District = CopyRequired(district, MaximumDistrictLength, "Address district");
        Neighborhood = CopyOptional(neighborhood, MaximumNeighborhoodLength, "Address neighborhood");
        FullAddress = CopyRequired(fullAddress, MaximumFullAddressLength, "Address full address");
        PostalCode = CopyOptional(postalCode, MaximumPostalCodeLength, "Address postal code");
    }

    // Burada snapshot alanlarını boşluk ve kalıcı depolama uzunluk sınırlarıyla kopyalıyorum.
    private static string CopyRequired(string value, int maximumLength, string fieldName)
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

    // Burada isteğe bağlı posta kodunu boşluk ve uzunluk sınırlarını koruyarak kopyalıyorum.
    private static string? CopyOptional(string? value, int maximumLength, string fieldName)
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
