using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class TaxRateAndShippingMethodTests
{
    // Burada vergi oranının adı temizleyip yüzde değerini geçerli sınırlar içinde sakladığını doğruluyorum.
    [Fact]
    public void TaxRate_Should_Normalize_Name_And_Allow_Boundary_Rates()
    {
        var taxRate = new TaxRate("  Genel KDV  ", TaxRate.MaximumRate);

        taxRate.Name.Should().Be("Genel KDV");
        taxRate.Rate.Should().Be(TaxRate.MaximumRate);
        taxRate.IsActive.Should().BeTrue();
    }

    // Burada sıfır ile yüz dışındaki vergi oranlarının domain seviyesinde reddedildiğini doğruluyorum.
    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void TaxRate_Should_Reject_Rate_Outside_Allowed_Range(decimal rate)
    {
        Action act = () => new TaxRate("KDV", rate);

        act.Should().Throw<DomainException>();
    }

    // Burada vergi dahil satış fiyatından vergi hariç fiyatın iki ondalık hassasiyetle üretildiğini doğruluyorum.
    [Fact]
    public void TaxRate_Should_Calculate_Net_Price_From_Tax_Inclusive_Price()
    {
        var taxRate = new TaxRate("KDV", 20m);

        var netPrice = taxRate.CalculateNetPrice(120m);

        netPrice.Should().Be(100m);
    }

    // Burada kargo yönteminin ücret ve gösterim sırası için negatif değerleri reddettiğini doğruluyorum.
    [Fact]
    public void ShippingMethod_Should_Reject_Negative_Fee_And_Display_Order()
    {
        Action negativeFee = () => new ShippingMethod("Standart", -0.01m);
        Action negativeOrder = () => new ShippingMethod("Standart", 0m, displayOrder: -1);

        negativeFee.Should().Throw<DomainException>();
        negativeOrder.Should().Throw<DomainException>();
    }

    // Burada kargo yönteminin ücret, sıralama ve aktiflik bilgilerinin yönetim işlemlerinde değiştiğini doğruluyorum.
    [Fact]
    public void ShippingMethod_Should_Update_Editable_Values_And_Activation()
    {
        var shippingMethod = new ShippingMethod("Standart", 49.90m, displayOrder: 1);

        shippingMethod.ChangeFixedFee(79.90m);
        shippingMethod.ChangeDisplayOrder(2);
        shippingMethod.Deactivate();

        shippingMethod.FixedFee.Should().Be(79.90m);
        shippingMethod.DisplayOrder.Should().Be(2);
        shippingMethod.IsActive.Should().BeFalse();
        shippingMethod.UpdatedAt.Should().NotBeNull();
    }
}
