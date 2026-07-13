using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class Address : AuditableEntity
{
    public Guid UserId { get; private set; }
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

    private Address()
    {
    }

    public Address(
        Guid userId,
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
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.");
        }

        UserId = userId;
        Type = type;
        SetTitle(title);
        SetName(firstName, lastName);
        SetPhone(phoneNumber);
        SetLocation(city, district, fullAddress);
        PostalCode = postalCode?.Trim();
        IsDefault = isDefault;
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        MarkAsUpdated();
    }

    public void UnsetDefault()
    {
        IsDefault = false;
        MarkAsUpdated();
    }

    private void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Address title cannot be empty.");
        }

        Title = title.Trim();
    }

    private void SetName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Address first and last name cannot be empty.");
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    private void SetPhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Phone number cannot be empty.");
        }

        PhoneNumber = phoneNumber.Trim();
    }

    private void SetLocation(string city, string district, string fullAddress)
    {
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(district) || string.IsNullOrWhiteSpace(fullAddress))
        {
            throw new DomainException("City, district and full address cannot be empty.");
        }

        City = city.Trim();
        District = district.Trim();
        FullAddress = fullAddress.Trim();
    }
}
