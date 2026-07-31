using ECommerce.Application.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.Common.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.PurchaseInvoices;

public sealed class PurchaseAccountingValidatorTests
{
    // Burada alış satırı fiyatı verilmediğinde sözleşmenin sıfırı kullanıp geçerli kaldığını doğruluyorum.
    [Fact]
    public void Purchase_Line_Should_Default_Entered_Price_To_Zero()
    {
        var input = new PurchaseInvoiceLineInput(
            1,
            Guid.NewGuid(),
            2m,
            "ADET",
            1m,
            PriceEntryMode.ExcludingVat,
            20m);

        var result = new PurchaseInvoiceLineInputValidator().Validate(input);

        input.EnteredUnitPrice.Should().Be(0m);
        result.IsValid.Should().BeTrue();
    }

    // Burada alış faturası başlığının para birimi verilmediğinde TRY ve birim kurla oluştuğunu doğruluyorum.
    [Fact]
    public void Purchase_Header_Should_Default_To_Try_And_Unit_Exchange_Rate()
    {
        var input = new PurchaseInvoiceHeaderInput(
            Guid.NewGuid(),
            "INV-DEFAULT",
            new DateTime(2026, 7, 26));

        var result = new PurchaseInvoiceHeaderInputValidator().Validate(input);

        input.CurrencyCode.Should().Be("TRY");
        input.ExchangeRate.Should().Be(1m);
        result.IsValid.Should().BeTrue();
    }

    // Burada bu milestone içinde TRY dışı para birimini ve birim olmayan kuru reddediyorum.
    [Theory]
    [InlineData("USD", "1")]
    [InlineData("TRY", "1.1")]
    public void Purchase_Header_Should_Reject_Unsupported_Currency_Or_Rate(
        string currencyCode,
        string exchangeRate)
    {
        var input = new PurchaseInvoiceHeaderInput(
            Guid.NewGuid(),
            "INV-CURRENCY",
            new DateTime(2026, 7, 26),
            CurrencyCode: currencyCode,
            ExchangeRate: decimal.Parse(
                exchangeRate,
                System.Globalization.CultureInfo.InvariantCulture));

        var result = new PurchaseInvoiceHeaderInputValidator().Validate(input);

        result.IsValid.Should().BeFalse();
    }

    // Burada ticari satır güncellemesinin sıfır varsayılan fiyatla geçerli olduğunu doğruluyorum.
    [Fact]
    public void Commercial_Line_Update_Should_Default_Entered_Price_To_Zero()
    {
        var input = new PurchaseInvoiceLineCommercialUpdateInput(
            3m,
            "KUTU",
            2m,
            PriceEntryMode.IncludingVat,
            20m);

        var result = new PurchaseInvoiceLineCommercialUpdateInputValidator().Validate(input);

        input.EnteredUnitPrice.Should().Be(0m);
        result.IsValid.Should().BeTrue();
    }
}
