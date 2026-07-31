using ECommerce.Domain.Accounting.Common;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.Common.ValueObjects;
using ECommerce.Domain.Common;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.Common;

public sealed class AccountingValueObjectTests
{
    // Burada para birimi kodunun boşluklardan arındırılıp büyük harfli kanonik biçimde saklandığını doğruluyorum.
    [Fact]
    public void CurrencyCode_Should_Normalize_Valid_Value()
    {
        var currencyCode = new CurrencyCode(" try ");

        currencyCode.Value.Should().Be("TRY");
        currencyCode.ToString().Should().Be("TRY");
    }

    // Burada aynı kanonik para birimi kodlarının değer eşitliği ve aynı hash değerini koruduğunu doğruluyorum.
    [Fact]
    public void CurrencyCode_Should_Use_Canonical_Value_Equality()
    {
        var first = new CurrencyCode("try");
        var second = new CurrencyCode(" TRY ");

        first.Should().Be(second);
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    // Burada boş, hatalı uzunlukta veya ASCII dışı para birimi kodlarının reddedildiğini doğruluyorum.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("TR")]
    [InlineData("TRYX")]
    [InlineData("T1Y")]
    [InlineData("TRİ")]
    public void CurrencyCode_Should_Reject_Invalid_Value(string? value)
    {
        var action = () => new CurrencyCode(value!);

        action.Should().Throw<DomainException>();
    }

    // Burada döviz kurunun altı ondalık ve decimal on sekiz-altı üst sınırında kabul edildiğini doğruluyorum.
    [Fact]
    public void ExchangeRate_Should_Accept_Supported_Boundaries()
    {
        var preciseRate = new ExchangeRate(1.123456m);
        var maximumRate = new ExchangeRate(AccountingPrecision.MaximumExchangeRate);

        preciseRate.Value.Should().Be(1.123456m);
        maximumRate.Value.Should().Be(AccountingPrecision.MaximumExchangeRate);
    }

    // Burada sıfır ve negatif döviz kurlarının reddedildiğini doğruluyorum.
    [Theory]
    [InlineData("0")]
    [InlineData("-0.000001")]
    public void ExchangeRate_Should_Reject_NonPositive_Value(string value)
    {
        var rate = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        var action = () => new ExchangeRate(rate);

        action.Should().Throw<DomainException>();
    }

    // Burada döviz kurunda fazla hassasiyetin sessizce yuvarlanmadan reddedildiğini doğruluyorum.
    [Fact]
    public void ExchangeRate_Should_Reject_Unsupported_Precision()
    {
        var action = () => new ExchangeRate(1.1234567m);

        action.Should().Throw<DomainException>();
    }

    // Burada döviz kurunun decimal on sekiz-altı veritabanı aralığını aşamadığını doğruluyorum.
    [Fact]
    public void ExchangeRate_Should_Reject_Value_Above_Supported_Maximum()
    {
        var action = () => new ExchangeRate(
            AccountingPrecision.MaximumExchangeRate + 0.000001m);

        action.Should().Throw<DomainException>();
    }

    // Burada fatura tutarının kanonik para birimiyle birlikte sıfır ve üst sınır değerlerini taşıdığını doğruluyorum.
    [Fact]
    public void InvoiceMoney_Should_Accept_Supported_Boundaries()
    {
        var currencyCode = new CurrencyCode("TRY");
        var zero = new InvoiceMoney(0m, currencyCode);
        var maximum = new InvoiceMoney(AccountingPrecision.MaximumInvoiceAmount, currencyCode);

        zero.Amount.Should().Be(0m);
        maximum.Amount.Should().Be(AccountingPrecision.MaximumInvoiceAmount);
        maximum.CurrencyCode.Should().Be(currencyCode);
    }

    // Burada aynı tutar ve para biriminin eşit, farklı para biriminin farklı değer olduğunu doğruluyorum.
    [Fact]
    public void InvoiceMoney_Should_Include_Currency_In_Value_Equality()
    {
        var first = new InvoiceMoney(100m, new CurrencyCode("TRY"));
        var same = new InvoiceMoney(100m, new CurrencyCode(" try "));
        var differentCurrency = new InvoiceMoney(100m, new CurrencyCode("USD"));

        first.Should().Be(same);
        first.GetHashCode().Should().Be(same.GetHashCode());
        first.Should().NotBe(differentCurrency);
    }

    // Burada aynı para birimindeki farklı fatura tutarlarının farklı değer nesneleri olduğunu doğruluyorum.
    [Fact]
    public void InvoiceMoney_Should_Include_Amount_In_Value_Equality()
    {
        var first = new InvoiceMoney(100m, new CurrencyCode("TRY"));
        var differentAmount = new InvoiceMoney(100.01m, new CurrencyCode("TRY"));

        first.Should().NotBe(differentAmount);
    }

    // Burada fatura tutarının para birimi olmadan oluşturulamadığını doğruluyorum.
    [Fact]
    public void InvoiceMoney_Should_Reject_Null_CurrencyCode()
    {
        var action = () => new InvoiceMoney(10m, null!);

        action.Should().Throw<DomainException>();
    }

    // Burada fatura tutarında ikiden fazla ondalığın sessizce yuvarlanmadan reddedildiğini doğruluyorum.
    [Fact]
    public void InvoiceMoney_Should_Reject_Unsupported_Precision()
    {
        var action = () => new InvoiceMoney(10.001m, new CurrencyCode("TRY"));

        action.Should().Throw<DomainException>();
    }

    // Burada negatif fatura tutarlarının ters kayıt gibi farklı kavramlarla karışmaması için reddedildiğini doğruluyorum.
    [Fact]
    public void InvoiceMoney_Should_Reject_Negative_Value()
    {
        var action = () => new InvoiceMoney(-0.01m, new CurrencyCode("TRY"));

        action.Should().Throw<DomainException>();
    }

    // Burada fatura tutarının desteklenen decimal on sekiz-iki üst sınırını aşamadığını doğruluyorum.
    [Fact]
    public void InvoiceMoney_Should_Reject_Value_Above_Supported_Range()
    {
        var action = () => new InvoiceMoney(
            AccountingPrecision.MaximumInvoiceAmount + 0.01m,
            new CurrencyCode("TRY"));

        action.Should().Throw<DomainException>();
    }

    // Burada KDV snapshot'ının sıfır, yüz ve dört ondalıklı oran sınırlarını kabul ettiğini doğruluyorum.
    [Fact]
    public void VatRateSnapshot_Should_Accept_Supported_Boundaries()
    {
        var zero = new VatRateSnapshot(0m);
        var precise = new VatRateSnapshot(20.1234m);
        var maximum = new VatRateSnapshot(100m);

        zero.Rate.Should().Be(0m);
        precise.Rate.Should().Be(20.1234m);
        maximum.Rate.Should().Be(100m);
    }

    // Burada KDV snapshot'ının sıfır-yüz aralığı dışındaki değerleri reddettiğini doğruluyorum.
    [Theory]
    [InlineData("-0.0001")]
    [InlineData("100.0001")]
    public void VatRateSnapshot_Should_Reject_OutOfRange_Value(string value)
    {
        var rate = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        var action = () => new VatRateSnapshot(rate);

        action.Should().Throw<DomainException>();
    }

    // Burada KDV snapshot'ında beşinci ondalığın sessizce yuvarlanmadan reddedildiğini doğruluyorum.
    [Fact]
    public void VatRateSnapshot_Should_Reject_Unsupported_Precision()
    {
        var action = () => new VatRateSnapshot(20.12345m);

        action.Should().Throw<DomainException>();
    }

    // Burada döviz kuru ve KDV snapshot'ının doğrulanmış değerleri üzerinden eşitlik kurduğunu doğruluyorum.
    [Fact]
    public void ScalarAccountingValueObjects_Should_Use_Value_Equality()
    {
        var firstRate = new ExchangeRate(1.123456m);
        var sameRate = new ExchangeRate(1.123456m);
        var firstVat = new VatRateSnapshot(20.1234m);
        var sameVat = new VatRateSnapshot(20.1234m);

        firstRate.Should().Be(sameRate);
        firstRate.GetHashCode().Should().Be(sameRate.GetHashCode());
        firstVat.Should().Be(sameVat);
        firstVat.GetHashCode().Should().Be(sameVat.GetHashCode());
    }

    // Burada muhasebe kaynak türlerinin sıfırdan farklı ve kalıcı sayısal sözleşmesini doğruluyorum.
    [Fact]
    public void AccountingSourceType_Should_Have_Stable_Contract_Values()
    {
        AccountingSourceType.PurchaseInvoice.Should().Be((AccountingSourceType)1);
        AccountingSourceType.SalesInvoice.Should().Be((AccountingSourceType)2);
        Enum.IsDefined(typeof(AccountingSourceType), 0).Should().BeFalse();
    }
}
