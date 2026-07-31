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
    public const int MaximumFullAddressLength = 500;
    public const int MaximumPostalCodeLength = 20;

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public Guid SourceAddressId { get; private set; }
    public AddressType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string District { get; private set; } = null!;
    public string FullAddress { get; private set; } = null!;
    public string? PostalCode { get; private set; }

    // Burada EF Core'un sipariş adresi snapshot'ını oluşturabilmesi için boş kurucuyu tutuyorum.
    private OrderAddressSnapshot()
    {
    }

    // Burada güvenilir kullanıcı adresini siparişe ait değişmez teslimat snapshot'ına dönüştürüyorum.
    internal OrderAddressSnapshot(Order order, Address address)
    {
        if (order is null || order.Id == Guid.Empty || address is null || address.Id == Guid.Empty)
        {
            throw new DomainException("Order and address are required.");
        }

        if (address.Type != AddressType.Shipping)
        {
            throw new DomainException("Only a shipping address can be attached to an order.");
        }

        OrderId = order.Id;
        Order = order;
        SourceAddressId = address.Id;
        Type = address.Type;
        Title = CopyRequired(address.Title, MaximumTitleLength, "Address title");
        FirstName = CopyRequired(address.FirstName, MaximumNameLength, "Address first name");
        LastName = CopyRequired(address.LastName, MaximumNameLength, "Address last name");
        PhoneNumber = CopyRequired(address.PhoneNumber, MaximumPhoneNumberLength, "Address phone number");
        City = CopyRequired(address.City, MaximumCityLength, "Address city");
        District = CopyRequired(address.District, MaximumDistrictLength, "Address district");
        FullAddress = CopyRequired(address.FullAddress, MaximumFullAddressLength, "Address full address");
        PostalCode = CopyOptional(address.PostalCode, MaximumPostalCodeLength, "Address postal code");
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
