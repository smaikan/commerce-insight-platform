using ECommerce.Application.Accounting.SalesOrders;
using ECommerce.Domain.Accounting.Common.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.SalesOrders;

public sealed class SalesAccountingValidatorTests
{
    // Burada satır indirimlerinin yalnız kendi kapsamlarına uygun UnitBasis birleşimleriyle kabul edildiğini doğruluyorum.
    [Theory]
    [InlineData(DiscountType.Percentage, null)]
    [InlineData(DiscountType.FixedLineTotal, null)]
    [InlineData(DiscountType.FixedPerUnit, DiscountUnitBasis.SaleUnit)]
    public void Line_Discount_Should_Accept_Scope_Appropriate_Combinations(
        DiscountType discountType,
        DiscountUnitBasis? unitBasis)
    {
        var result = new AccountingSalesOrderLineInputValidator()
            .Validate(CreateLine(discountType, unitBasis));

        result.IsValid.Should().BeTrue();
    }

    // Burada eksik UnitBasis, gereksiz UnitBasis ve fatura kapsamlı satır indiriminin reddedildiğini doğruluyorum.
    [Theory]
    [InlineData(DiscountType.FixedPerUnit, null)]
    [InlineData(DiscountType.Percentage, DiscountUnitBasis.StockUnit)]
    [InlineData(DiscountType.FixedLineTotal, DiscountUnitBasis.SaleUnit)]
    [InlineData(DiscountType.FixedInvoiceTotal, null)]
    public void Line_Discount_Should_Reject_Incomplete_Or_Inappropriate_Combinations(
        DiscountType discountType,
        DiscountUnitBasis? unitBasis)
    {
        var result = new AccountingSalesOrderLineInputValidator()
            .Validate(CreateLine(discountType, unitBasis));

        result.IsValid.Should().BeFalse();
    }

    // Burada başlık indiriminin yalnız yüzde veya sabit fatura toplamı türleriyle kabul edildiğini doğruluyorum.
    [Theory]
    [InlineData(DiscountType.Percentage)]
    [InlineData(DiscountType.FixedInvoiceTotal)]
    public void Invoice_Discount_Should_Accept_Invoice_Scope_Types(DiscountType discountType)
    {
        var result = new AccountingSalesOrderHeaderInputValidator()
            .Validate(CreateHeader(discountType, 10m, DiscountTaxBasis.ExcludingVat));

        result.IsValid.Should().BeTrue();
    }

    // Burada satır kapsamlı veya eksik fatura başlık indirimi birleşimlerinin reddedildiğini doğruluyorum.
    [Fact]
    public void Invoice_Discount_Should_Reject_Line_Scope_And_Incomplete_Combinations()
    {
        var validator = new AccountingSalesOrderHeaderInputValidator();

        var fixedPerUnit = validator.Validate(
            CreateHeader(DiscountType.FixedPerUnit, 10m, DiscountTaxBasis.ExcludingVat));
        var fixedLineTotal = validator.Validate(
            CreateHeader(DiscountType.FixedLineTotal, 10m, DiscountTaxBasis.ExcludingVat));
        var incomplete = validator.Validate(
            CreateHeader(DiscountType.Percentage, 10m, null));

        fixedPerUnit.IsValid.Should().BeFalse();
        fixedLineTotal.IsValid.Should().BeFalse();
        incomplete.IsValid.Should().BeFalse();
    }

    // Burada satış başlığının yalnız TRY/1 para sözleşmesini ve kargo ödeyen taraf eşleşmesini kabul ettiğini doğruluyorum.
    [Theory]
    [InlineData("USD", 1, 0, ShippingPayer.None, false)]
    [InlineData("TRY", 2, 0, ShippingPayer.None, false)]
    [InlineData("TRY", 1, 0, ShippingPayer.Customer, false)]
    [InlineData("TRY", 1, 25, ShippingPayer.None, false)]
    [InlineData("TRY", 1, 25, ShippingPayer.Seller, true)]
    [InlineData("TRY", 1, 25, ShippingPayer.Customer, true)]
    public void Header_Should_Enforce_Currency_And_Shipping_Payer(
        string currencyCode,
        int exchangeRate,
        int shippingTotal,
        ShippingPayer shippingPayer,
        bool expectedValid)
    {
        var input = CreateHeader(null, null, null) with
        {
            CurrencyCode = currencyCode,
            ExchangeRate = exchangeRate,
            ShippingTotal = shippingTotal,
            ShippingPayer = shippingPayer
        };

        var result = new AccountingSalesOrderHeaderInputValidator().Validate(input);

        result.IsValid.Should().Be(expectedValid);
    }

    // Burada açıkça gönderilmeyen satış fiyatı ile başlık alanlarının onaylı sıfır ve TRY varsayılanlarına düştüğünü doğruluyorum.
    [Fact]
    public void Sales_Inputs_Should_Expose_Approved_Defaults()
    {
        var line = new AccountingSalesOrderLineInput(
            1,
            Guid.NewGuid(),
            1m,
            "ADET",
            1m,
            PriceEntryMode.ExcludingVat,
            20m);
        var header = new AccountingSalesOrderHeaderInput(
            Guid.NewGuid(),
            "SAL-DEFAULTS",
            new DateTime(2026, 7, 26));

        line.EnteredUnitPrice.Should().Be(0m);
        header.CurrencyCode.Should().Be("TRY");
        header.ExchangeRate.Should().Be(1m);
        header.ShippingTotal.Should().Be(0m);
        header.ShippingPayer.Should().Be(ShippingPayer.None);
    }

    // Burada fatura satırı güncelleme payload'ının varyant ve tarihsel kod snapshot alanlarını yayımlamadığını doğruluyorum.
    [Fact]
    public void Invoice_Line_Update_Input_Should_Not_Expose_Product_Identity_Or_Codes()
    {
        var propertyNames = typeof(SalesInvoiceLineUpdateInput)
            .GetProperties()
            .Select(property => property.Name);

        propertyNames.Should().NotContain(
            ["ProductId", "ProductVariantId", "ProductName", "VariantName", "Sku", "Barcode"]);
    }

    // Burada doğrulama senaryoları için geçerli temel alanlarla bir satış satırı oluşturuyorum.
    private static AccountingSalesOrderLineInput CreateLine(
        DiscountType discountType,
        DiscountUnitBasis? unitBasis)
    {
        return new AccountingSalesOrderLineInput(
            1,
            Guid.NewGuid(),
            1m,
            "ADET",
            1m,
            PriceEntryMode.ExcludingVat,
            20m,
            100m,
            discountType,
            10m,
            DiscountTaxBasis.ExcludingVat,
            unitBasis);
    }

    // Burada doğrulama senaryoları için açık indirim alanları taşıyan satış başlığı oluşturuyorum.
    private static AccountingSalesOrderHeaderInput CreateHeader(
        DiscountType? discountType,
        decimal? discountValue,
        DiscountTaxBasis? taxBasis)
    {
        return new AccountingSalesOrderHeaderInput(
            Guid.NewGuid(),
            "SAL-VALIDATION",
            new DateTime(2026, 7, 26),
            new DateTime(2026, 8, 26),
            "TRY",
            1m,
            0m,
            null,
            discountType,
            discountValue,
            taxBasis);
    }
}
