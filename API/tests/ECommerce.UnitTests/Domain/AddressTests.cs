using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class AddressTests
{
    // Burada adres alanlarının boşluklardan arındırıldığını ve boş posta kodunun null saklandığını doğruluyorum.
    [Fact]
    public void Constructor_Should_Normalize_Address_Fields()
    {
        var address = CreateAddress(
            title: "  Ev  ",
            city: "  İstanbul  ",
            postalCode: "   ");

        address.Title.Should().Be("Ev");
        address.City.Should().Be("İstanbul");
        address.PostalCode.Should().BeNull();
    }

    // Burada tanımsız adres türüyle kayıt oluşturulmasının Domain seviyesinde engellendiğini doğruluyorum.
    [Fact]
    public void Constructor_Should_Reject_Undefined_Address_Type()
    {
        Action act = () => CreateAddress(type: (AddressType)99);

        act.Should().Throw<DomainException>();
    }

    // Burada veritabanı sözleşmesini aşan adres alanının daha kalıcı kayda gitmeden reddedildiğini doğruluyorum.
    [Fact]
    public void Constructor_Should_Reject_Too_Long_Full_Address()
    {
        Action act = () => CreateAddress(fullAddress: new string('A', Address.MaximumFullAddressLength + 1));

        act.Should().Throw<DomainException>();
    }

    // Burada adres güncellemesinin sahibini değiştirmeden yeni bilgileri ve güncelleme tarihini uyguladığını doğruluyorum.
    [Fact]
    public void Update_Should_Keep_Owner_And_Change_Details()
    {
        var address = CreateAddress();

        address.Update(
            AddressType.Billing,
            "İş",
            "Ada",
            "Yılmaz",
            "05000000000",
            "Ankara",
            "Çankaya",
            "Yeni adres",
            "06000");

        address.UserId.Should().Be(7);
        address.Type.Should().Be(AddressType.Billing);
        address.Title.Should().Be("İş");
        address.UpdatedAt.Should().NotBeNull();
    }

    // Burada testler için geçerli ve isteğe göre değiştirilebilir bir adres oluşturuyorum.
    private static Address CreateAddress(
        AddressType type = AddressType.Shipping,
        string title = "Ev",
        string city = "İzmir",
        string fullAddress = "Alsancak Mahallesi 1. Sokak No: 1",
        string? postalCode = "35220")
    {
        return new Address(
            7,
            type,
            title,
            "Ada",
            "Yılmaz",
            "05000000000",
            city,
            "Konak",
            fullAddress,
            postalCode);
    }
}
