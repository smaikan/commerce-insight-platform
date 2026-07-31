using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Domain.Accounting.Common.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.Common;

public sealed class AccountingRoundingPolicyTests
{
    // Burada parasal yarım değerlerin iki ondalıkta sıfırdan uzağa yuvarlandığını doğruluyorum.
    [Fact]
    public void RoundMoney_Should_Use_AwayFromZero_At_Two_Decimals()
    {
        var policy = new AccountingRoundingPolicy();

        var positive = policy.RoundMoney(1.005m);
        var negative = policy.RoundMoney(-1.005m);

        positive.Should().Be(1.01m);
        negative.Should().Be(-1.01m);
    }

    // Burada birim fiyat, miktar ve yüzde değerlerinin ortak dört ondalıklı hassasiyeti kullandığını doğruluyorum.
    [Fact]
    public void RoundingPolicy_Should_Use_Four_Decimals_For_NonTotal_Values()
    {
        var policy = new AccountingRoundingPolicy();

        policy.RoundUnitPrice(1.23445m).Should().Be(1.2345m);
        policy.RoundQuantity(2.34565m).Should().Be(2.3457m);
        policy.RoundPercentage(3.45675m).Should().Be(3.4568m);
    }

    // Burada ortak fatura enumlarının kararlı ve sıfırdan farklı sözleşme değerlerini koruduğunu doğruluyorum.
    [Fact]
    public void SharedInvoiceEnums_Should_Have_Stable_Contract_Values()
    {
        InvoiceStatus.Draft.Should().Be((InvoiceStatus)1);
        InvoiceStatus.Posted.Should().Be((InvoiceStatus)2);
        InvoiceStatus.Cancelled.Should().Be((InvoiceStatus)3);
        PriceEntryMode.ExcludingVat.Should().Be((PriceEntryMode)1);
        PriceEntryMode.IncludingVat.Should().Be((PriceEntryMode)2);
        DiscountScope.Line.Should().Be((DiscountScope)1);
        DiscountScope.Invoice.Should().Be((DiscountScope)2);
        DiscountType.Percentage.Should().Be((DiscountType)1);
        DiscountType.FixedPerUnit.Should().Be((DiscountType)2);
        DiscountType.FixedLineTotal.Should().Be((DiscountType)3);
        DiscountType.FixedInvoiceTotal.Should().Be((DiscountType)4);
        DiscountTaxBasis.ExcludingVat.Should().Be((DiscountTaxBasis)1);
        DiscountTaxBasis.IncludingVat.Should().Be((DiscountTaxBasis)2);
        DiscountUnitBasis.PurchaseUnit.Should().Be((DiscountUnitBasis)1);
        DiscountUnitBasis.SaleUnit.Should().Be((DiscountUnitBasis)2);
        DiscountUnitBasis.StockUnit.Should().Be((DiscountUnitBasis)3);
    }
}
