using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

// Burada sipariş anındaki müşteri iletişim bilgilerini kullanıcı hesabından bağımsız ve değişmez olarak saklıyorum.
public sealed class OrderCustomerSnapshot : BaseEntity
{
    public const int MaximumNameLength = 100;
    public const int MaximumEmailLength = 320;
    public const int MaximumPhoneNumberLength = 30;

    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;

    // Burada EF Core'un müşteri snapshot kaydını oluşturabilmesi için boş kurucuyu tutuyorum.
    private OrderCustomerSnapshot()
    {
    }

    // Burada güvenilir müşteri bilgilerini siparişe ait değişmez snapshot'a dönüştürüyorum.
    internal OrderCustomerSnapshot(
        Order order,
        string firstName,
        string lastName,
        string email,
        string phoneNumber)
    {
        if (order is null || order.Id == Guid.Empty)
        {
            throw new DomainException("Order is required.");
        }

        OrderId = order.Id;
        Order = order;
        FirstName = NormalizeRequired(firstName, MaximumNameLength, "Customer first name");
        LastName = NormalizeRequired(lastName, MaximumNameLength, "Customer last name");
        Email = NormalizeEmail(email);
        PhoneNumber = NormalizeRequired(phoneNumber, MaximumPhoneNumberLength, "Customer phone number");
    }

    // Burada zorunlu müşteri alanlarını boşluk ve uzunluk sınırlarıyla normalize ediyorum.
    private static string NormalizeRequired(string value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} cannot be empty.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maximumLength} characters.");
        }

        return normalized;
    }

    // Burada müşteri e-postasını karşılaştırılabilir küçük harfli biçimde saklıyorum.
    private static string NormalizeEmail(string email)
    {
        var normalized = NormalizeRequired(email, MaximumEmailLength, "Customer email").ToLowerInvariant();
        if (!normalized.Contains('@', StringComparison.Ordinal))
        {
            throw new DomainException("Customer email is invalid.");
        }

        return normalized;
    }
}
