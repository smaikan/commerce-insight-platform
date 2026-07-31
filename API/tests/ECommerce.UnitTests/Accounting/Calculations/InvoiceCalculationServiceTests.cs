using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Common;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.Calculations;

public sealed class InvoiceCalculationServiceTests
{
    // Burada KDV hariç girilen fiyatın iki KDV karşılığı ve satır toplamlarının doğru hesaplandığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Calculate_VatExclusive_Price_And_Amounts()
    {
        var service = CreateService();

        var result = service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, quantity: 2m, enteredUnitPrice: 100m, vatRate: 20m)]));

        var line = result.Lines.Single();
        line.UnitPriceExcludingVat.Should().Be(100m);
        line.UnitPriceIncludingVat.Should().Be(120m);
        line.GrossAmountExcludingVat.Should().Be(200m);
        line.GrossAmountIncludingVat.Should().Be(240m);
        line.NetAmountExcludingVat.Should().Be(200m);
        line.VatAmount.Should().Be(40m);
        line.TotalAmountIncludingVat.Should().Be(240m);
    }

    // Burada KDV dahil girilen fiyatın vergi hariç karşılığının doğru ayrıştırıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Calculate_VatInclusive_Price_And_Amounts()
    {
        var service = CreateService();

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    quantity: 2m,
                    enteredUnitPrice: 120m,
                    vatRate: 20m,
                    priceEntryMode: PriceEntryMode.IncludingVat)
            ]));

        var line = result.Lines.Single();
        line.UnitPriceExcludingVat.Should().Be(100m);
        line.UnitPriceIncludingVat.Should().Be(120m);
        line.GrossAmountExcludingVat.Should().Be(200m);
        line.GrossAmountIncludingVat.Should().Be(240m);
        line.VatAmount.Should().Be(40m);
    }

    // Burada KDV hariç baza uygulanan yüzde satır indiriminin iki vergi karşılığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Apply_Percentage_Line_Discount_On_Exclusive_Base()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: discount)]));

        var line = result.Lines.Single();
        line.LineDiscountAmountExcludingVat.Should().Be(10m);
        line.LineDiscountAmountIncludingVat.Should().Be(12m);
        line.TotalDiscountAmountExcludingVat.Should().Be(10m);
        line.NetAmountExcludingVat.Should().Be(90m);
        line.VatAmount.Should().Be(18m);
        line.TotalAmountIncludingVat.Should().Be(108m);
    }

    // Burada KDV dahil baza uygulanan yüzde satır indiriminin vergi hariç karşılığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Apply_Percentage_Line_Discount_On_Inclusive_Base()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.IncludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    enteredUnitPrice: 120m,
                    vatRate: 20m,
                    priceEntryMode: PriceEntryMode.IncludingVat,
                    lineDiscount: discount)
            ]));

        var line = result.Lines.Single();
        line.LineDiscountAmountIncludingVat.Should().Be(12m);
        line.LineDiscountAmountExcludingVat.Should().Be(10m);
        line.NetAmountExcludingVat.Should().Be(90m);
        line.TotalAmountIncludingVat.Should().Be(108m);
    }

    // Burada alış birimi başına sabit indirimin yalnız belge miktarıyla çarpıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Apply_FixedPerUnit_Discount_On_PurchaseUnits()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.FixedPerUnit,
            5m,
            DiscountTaxBasis.ExcludingVat,
            DiscountUnitBasis.PurchaseUnit);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    quantity: 5m,
                    unitsPerUnit: 12m,
                    enteredUnitPrice: 100m,
                    lineDiscount: discount)
            ]));

        var line = result.Lines.Single();
        line.StockQuantity.Should().Be(60m);
        line.LineDiscountAmountExcludingVat.Should().Be(25m);
        line.LineDiscountAmountIncludingVat.Should().Be(30m);
        line.NetAmountExcludingVat.Should().Be(475m);
    }

    // Burada satış birimi seçiminin ortak motor içinde belge miktarını esas aldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Apply_FixedPerUnit_Discount_On_SaleUnits()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.FixedPerUnit,
            5m,
            DiscountTaxBasis.ExcludingVat,
            DiscountUnitBasis.SaleUnit);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    quantity: 5m,
                    unitsPerUnit: 12m,
                    enteredUnitPrice: 100m,
                    lineDiscount: discount)
            ]));

        result.Lines.Single().LineDiscountAmountExcludingVat.Should().Be(25m);
    }

    // Burada stok birimi başına sabit indirimin dönüştürülmüş stok miktarıyla çarpıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Apply_FixedPerUnit_Discount_On_StockUnits()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.FixedPerUnit,
            5m,
            DiscountTaxBasis.ExcludingVat,
            DiscountUnitBasis.StockUnit);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    quantity: 5m,
                    unitsPerUnit: 12m,
                    enteredUnitPrice: 100m,
                    lineDiscount: discount)
            ]));

        var line = result.Lines.Single();
        line.LineDiscountAmountExcludingVat.Should().Be(300m);
        line.LineDiscountAmountIncludingVat.Should().Be(360m);
        line.NetAmountExcludingVat.Should().Be(200m);
    }

    // Burada KDV dahil birim başına indirimin satırın kendi vergi oranıyla hariç tutara ayrıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Apply_FixedPerUnit_Discount_On_Inclusive_Base()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.FixedPerUnit,
            12m,
            DiscountTaxBasis.IncludingVat,
            DiscountUnitBasis.SaleUnit);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    enteredUnitPrice: 120m,
                    vatRate: 20m,
                    priceEntryMode: PriceEntryMode.IncludingVat,
                    lineDiscount: discount)
            ]));

        var line = result.Lines.Single();
        line.LineDiscountAmountIncludingVat.Should().Be(12m);
        line.LineDiscountAmountExcludingVat.Should().Be(10m);
        line.NetAmountExcludingVat.Should().Be(90m);
    }

    // Burada KDV hariç sabit satır toplamı indiriminin karşı vergi tutarını doğru ürettiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Apply_FixedLineTotal_On_Exclusive_Base()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.FixedLineTotal,
            10m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: discount)]));

        var line = result.Lines.Single();
        line.LineDiscountAmountExcludingVat.Should().Be(10m);
        line.LineDiscountAmountIncludingVat.Should().Be(12m);
        line.NetAmountExcludingVat.Should().Be(90m);
        line.TotalAmountIncludingVat.Should().Be(108m);
    }

    // Burada KDV dahil sabit satır indiriminin KDV hariç tutara doğru ayrıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Separate_FixedLineTotal_Entered_IncludingVat()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.FixedLineTotal,
            12m,
            DiscountTaxBasis.IncludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    enteredUnitPrice: 120m,
                    vatRate: 20m,
                    priceEntryMode: PriceEntryMode.IncludingVat,
                    lineDiscount: discount)
            ]));

        var line = result.Lines.Single();
        line.LineDiscountAmountIncludingVat.Should().Be(12m);
        line.LineDiscountAmountExcludingVat.Should().Be(10m);
        line.NetAmountExcludingVat.Should().Be(90m);
        line.VatAmount.Should().Be(18m);
        line.TotalAmountIncludingVat.Should().Be(108m);
    }

    // Burada aynı faturadaki farklı KDV oranlarının satır ve başlık seviyesinde ayrı hesaplandığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Support_Mixed_Vat_Rates()
    {
        var service = CreateService();

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(1, enteredUnitPrice: 100m, vatRate: 20m),
                CreateLine(2, enteredUnitPrice: 100m, vatRate: 10m)
            ]));

        result.Lines[0].VatAmount.Should().Be(20m);
        result.Lines[1].VatAmount.Should().Be(10m);
        result.Totals.SubtotalExcludingVat.Should().Be(200m);
        result.Totals.SubtotalIncludingVat.Should().Be(230m);
        result.Totals.NetAmountExcludingVat.Should().Be(200m);
        result.Totals.VatTotal.Should().Be(30m);
        result.Totals.GrandTotalIncludingVat.Should().Be(230m);
    }

    // Burada yüzde yüz satır indiriminin negatif tutar üretmeden ücretsiz satıra izin verdiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Allow_OneHundredPercent_Line_Discount()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.Percentage,
            100m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: discount)]));

        var line = result.Lines.Single();
        line.TotalDiscountAmountExcludingVat.Should().Be(100m);
        line.TotalDiscountAmountIncludingVat.Should().Be(120m);
        line.NetAmountExcludingVat.Should().Be(0m);
        line.VatAmount.Should().Be(0m);
        line.TotalAmountIncludingVat.Should().Be(0m);
    }

    // Burada sıfır KDV oranında dahil ve hariç bütün tutarların eşit kaldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Keep_Amounts_Equal_When_Vat_Is_Zero()
    {
        var service = CreateService();

        var result = service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, enteredUnitPrice: 80m, vatRate: 0m)]));

        var line = result.Lines.Single();
        line.UnitPriceExcludingVat.Should().Be(80m);
        line.UnitPriceIncludingVat.Should().Be(80m);
        line.GrossAmountExcludingVat.Should().Be(80m);
        line.GrossAmountIncludingVat.Should().Be(80m);
        line.VatAmount.Should().Be(0m);
    }

    // Burada null veya satırsız fatura girdilerinin hesap motoruna alınmadığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Null_And_Empty_Invoice_Input()
    {
        var service = CreateService();

        Action nullInput = () => service.Calculate(null!);
        Action emptyInput = () => service.Calculate(new InvoiceCalculationInput([]));

        nullInput.Should().Throw<DomainException>();
        emptyInput.Should().Throw<DomainException>();
    }

    // Burada sıfır veya negatif miktar ile birim dönüşüm değerlerinin reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_NonPositive_Quantity_Inputs()
    {
        var service = CreateService();

        Action zeroQuantity = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, quantity: 0m)]));
        Action negativeUnits = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, unitsPerUnit: -1m)]));

        zeroQuantity.Should().Throw<DomainException>();
        negativeUnits.Should().Throw<DomainException>();
    }

    // Burada negatif fiyat ve KDV oranının güvenilir hesaplamadan önce reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Negative_Price_And_Vat()
    {
        var service = CreateService();

        Action negativePrice = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, enteredUnitPrice: -0.01m)]));
        Action negativeVat = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, vatRate: -0.0001m)]));

        negativePrice.Should().Throw<DomainException>();
        negativeVat.Should().Throw<DomainException>();
    }

    // Burada proje TaxRate sınırıyla uyumlu olarak yüzde yüzü aşan KDV oranının reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Vat_Above_OneHundred()
    {
        var service = CreateService();

        Action act = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, vatRate: 100.0001m)]));

        act.Should().Throw<DomainException>();
    }

    // Burada yüzde indirimin sıfır ile yüz dışındaki değerlerinin reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Percentage_Outside_Allowed_Range()
    {
        var service = CreateService();
        var negativeDiscount = CreateLineDiscount(
            DiscountType.Percentage,
            -0.0001m,
            DiscountTaxBasis.ExcludingVat);
        var excessiveDiscount = CreateLineDiscount(
            DiscountType.Percentage,
            100.0001m,
            DiscountTaxBasis.ExcludingVat);

        Action negative = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: negativeDiscount)]));
        Action excessive = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: excessiveDiscount)]));

        negative.Should().Throw<DomainException>();
        excessive.Should().Throw<DomainException>();
    }

    // Burada sabit satır indiriminin seçilen brüt tabanı aşamadığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Fixed_Line_Discount_Above_Base()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.FixedLineTotal,
            100.01m,
            DiscountTaxBasis.ExcludingVat);

        Action act = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: discount)]));

        act.Should().Throw<DomainException>();
    }

    // Burada yuvarlandığında tabana eşit görünse bile ham birim indirimi aşımının reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Raw_FixedPerUnit_Discount_Above_Base()
    {
        var service = CreateService();
        var discount = CreateLineDiscount(
            DiscountType.FixedPerUnit,
            100.004m,
            DiscountTaxBasis.ExcludingVat,
            DiscountUnitBasis.SaleUnit);

        Action act = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, enteredUnitPrice: 100m, lineDiscount: discount)]));

        act.Should().Throw<DomainException>();
    }

    // Burada satır ve fatura scope'larında desteklenmeyen indirim türlerinin reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Discount_Type_Outside_Its_Scope()
    {
        var service = CreateService();
        var invalidLineDiscount = CreateLineDiscount(
            DiscountType.FixedInvoiceTotal,
            10m,
            DiscountTaxBasis.ExcludingVat);
        var invalidInvoiceDiscount = new DiscountCalculationInput(
            DiscountScope.Invoice,
            DiscountType.FixedLineTotal,
            10m,
            DiscountTaxBasis.ExcludingVat);

        Action lineAction = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: invalidLineDiscount)]));
        Action invoiceAction = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1)],
            invalidInvoiceDiscount));

        lineAction.Should().Throw<DomainException>();
        invoiceAction.Should().Throw<DomainException>();
    }

    // Burada FixedPerUnit indirimin zorunlu birim bazını ve diğer türlerin boş birim bazını koruduğunu doğruluyorum.
    [Fact]
    public void Calculate_Should_Validate_Discount_UnitBasis()
    {
        var service = CreateService();
        var missingUnitBasis = CreateLineDiscount(
            DiscountType.FixedPerUnit,
            1m,
            DiscountTaxBasis.ExcludingVat);
        var unexpectedUnitBasis = CreateLineDiscount(
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.ExcludingVat,
            DiscountUnitBasis.StockUnit);

        Action missing = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: missingUnitBasis)]));
        Action unexpected = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: unexpectedUnitBasis)]));

        missing.Should().Throw<DomainException>();
        unexpected.Should().Throw<DomainException>();
    }

    // Burada aynı satır numarasının deterministik sonuç eşlemesini bozmadan reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Duplicate_LineNumbers()
    {
        var service = CreateService();

        Action act = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1), CreateLine(1)]));

        act.Should().Throw<DomainException>();
    }

    // Burada tanımsız fiyat ve indirim enum değerlerinin hesaplamaya giremediğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Undefined_Enum_Values()
    {
        var service = CreateService();
        var invalidTaxBasis = new DiscountCalculationInput(
            DiscountScope.Line,
            DiscountType.Percentage,
            10m,
            (DiscountTaxBasis)999);

        Action priceMode = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, priceEntryMode: (PriceEntryMode)999)]));
        Action taxBasis = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: invalidTaxBasis)]));

        priceMode.Should().Throw<DomainException>();
        taxBasis.Should().Throw<DomainException>();
    }

    // Burada tanımsız indirim türü ve birim bazı değerlerinin reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Undefined_Discount_Type_And_UnitBasis()
    {
        var service = CreateService();
        var invalidType = new DiscountCalculationInput(
            DiscountScope.Line,
            (DiscountType)999,
            10m,
            DiscountTaxBasis.ExcludingVat);
        var invalidUnitBasis = new DiscountCalculationInput(
            DiscountScope.Line,
            DiscountType.FixedPerUnit,
            1m,
            DiscountTaxBasis.ExcludingVat,
            (DiscountUnitBasis)999);

        Action typeAction = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: invalidType)]));
        Action unitAction = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: invalidUnitBasis)]));

        typeAction.Should().Throw<DomainException>();
        unitAction.Should().Throw<DomainException>();
    }

    // Burada satır ve fatura indirimlerinde yanlış scope kullanılmasının reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Discount_Scope_Mismatch()
    {
        var service = CreateService();
        var invoiceScopedLineDiscount = new DiscountCalculationInput(
            DiscountScope.Invoice,
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.ExcludingVat);
        var lineScopedInvoiceDiscount = new DiscountCalculationInput(
            DiscountScope.Line,
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.ExcludingVat);

        Action lineAction = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: invoiceScopedLineDiscount)]));
        Action invoiceAction = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1)],
            lineScopedInvoiceDiscount));

        lineAction.Should().Throw<DomainException>();
        invoiceAction.Should().Throw<DomainException>();
    }

    // Burada null satır ile pozitif olmayan satır numarasının reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Null_Line_And_NonPositive_LineNumber()
    {
        var service = CreateService();

        Action nullLine = () => service.Calculate(new InvoiceCalculationInput(
            new InvoiceLineCalculationInput[] { null! }));
        Action zeroLineNumber = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(0)]));

        nullLine.Should().Throw<DomainException>();
        zeroLineNumber.Should().Throw<DomainException>();
    }

    // Burada miktar, fiyat, yüzde ve sabit tutar girdilerinin ortak hassasiyeti aşamadığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Input_Precision_Above_Supported_Scales()
    {
        var service = CreateService();
        var impreciseDiscount = CreateLineDiscount(
            DiscountType.FixedLineTotal,
            0.001m,
            DiscountTaxBasis.ExcludingVat);

        Action quantity = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, quantity: 1.00001m)]));
        Action unitPrice = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, enteredUnitPrice: 1.00001m)]));
        Action vatRate = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, vatRate: 20.00001m)]));
        Action fixedTotal = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, lineDiscount: impreciseDiscount)]));

        quantity.Should().Throw<DomainException>();
        unitPrice.Should().Throw<DomainException>();
        vatRate.Should().Throw<DomainException>();
        fixedTotal.Should().Throw<DomainException>();
    }

    // Burada decimal sınırını aşan çarpımların kontrollü domain hatasına çevrildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Convert_Decimal_Overflow_To_DomainException()
    {
        var service = CreateService();

        Action act = () => service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    quantity: 2m,
                    enteredUnitPrice: decimal.MaxValue,
                    vatRate: 0m)
            ]));

        act.Should().Throw<DomainException>()
            .WithMessage("*supported decimal range*");
    }

    // Burada testlerin hesap motorunu gerçek merkezi yuvarlama politikasıyla kurmasını sağlıyorum.
    private static InvoiceCalculationService CreateService()
    {
        return new InvoiceCalculationService(new AccountingRoundingPolicy());
    }

    // Burada test satırını güvenli varsayılanlarla ve gerektiğinde değiştirilebilir ham girdilerle hazırlıyorum.
    private static InvoiceLineCalculationInput CreateLine(
        int lineNumber,
        decimal quantity = 1m,
        decimal unitsPerUnit = 1m,
        decimal enteredUnitPrice = 100m,
        decimal vatRate = 20m,
        PriceEntryMode priceEntryMode = PriceEntryMode.ExcludingVat,
        DiscountCalculationInput? lineDiscount = null,
        bool isInvoiceDiscountEligible = true)
    {
        return new InvoiceLineCalculationInput(
            lineNumber,
            quantity,
            unitsPerUnit,
            enteredUnitPrice,
            priceEntryMode,
            vatRate,
            lineDiscount,
            isInvoiceDiscountEligible);
    }

    // Burada test için satır scope'lu indirim tanımını tek yerden hazırlıyorum.
    private static DiscountCalculationInput CreateLineDiscount(
        DiscountType type,
        decimal value,
        DiscountTaxBasis taxBasis,
        DiscountUnitBasis? unitBasis = null)
    {
        return new DiscountCalculationInput(
            DiscountScope.Line,
            type,
            value,
            taxBasis,
            unitBasis);
    }
}
