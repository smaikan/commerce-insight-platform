using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Common;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.Calculations;

public sealed class InvoiceDiscountDistributionTests
{
    // Burada yüzde fatura indiriminin bütün uygun satırlara aynı oranla dağıtıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Distribute_Percentage_Invoice_Discount()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(1, enteredUnitPrice: 1000m),
                CreateLine(2, enteredUnitPrice: 500m)
            ],
            invoiceDiscount));

        result.Lines[0].InvoiceDiscountShareExcludingVat.Should().Be(100m);
        result.Lines[1].InvoiceDiscountShareExcludingVat.Should().Be(50m);
        result.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(150m);
        result.Totals.InvoiceDiscountTotalIncludingVat.Should().Be(180m);
    }

    // Burada yüzde fatura indiriminin yuvarlanmış hedef yerine her satırın kendi yüzdesinden üretildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Apply_Same_Percentage_Before_FinalLine_Reconciliation()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.Percentage,
            50m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(1, enteredUnitPrice: 100m, vatRate: 0m),
                CreateLine(2, enteredUnitPrice: 0.01m, vatRate: 0m)
            ],
            invoiceDiscount));

        result.Lines[0].InvoiceDiscountShareExcludingVat.Should().Be(50m);
        result.Lines[1].InvoiceDiscountShareExcludingVat.Should().Be(0.01m);
        result.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(50.01m);
    }

    // Burada KDV dahil yüzde fatura indiriminin her satırın KDV oranıyla hariç tutara çevrildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Distribute_Inclusive_Percentage_Invoice_Discount()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.IncludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    enteredUnitPrice: 120m,
                    vatRate: 20m,
                    priceEntryMode: PriceEntryMode.IncludingVat),
                CreateLine(
                    2,
                    enteredUnitPrice: 110m,
                    vatRate: 10m,
                    priceEntryMode: PriceEntryMode.IncludingVat)
            ],
            invoiceDiscount));

        result.Lines[0].InvoiceDiscountShareIncludingVat.Should().Be(12m);
        result.Lines[1].InvoiceDiscountShareIncludingVat.Should().Be(11m);
        result.Lines[0].InvoiceDiscountShareExcludingVat.Should().Be(10m);
        result.Lines[1].InvoiceDiscountShareExcludingVat.Should().Be(10m);
    }

    // Burada sabit fatura indiriminin eşit değil uygun satır tutarları oranında dağıtıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Distribute_Fixed_Invoice_Discount_Proportionally()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            300m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(1, enteredUnitPrice: 1000m),
                CreateLine(2, enteredUnitPrice: 500m)
            ],
            invoiceDiscount));

        result.Lines[0].InvoiceDiscountShareExcludingVat.Should().Be(200m);
        result.Lines[1].InvoiceDiscountShareExcludingVat.Should().Be(100m);
        result.Lines.Sum(line => line.InvoiceDiscountShareExcludingVat).Should().Be(300m);
        result.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(300m);
    }

    // Burada fatura indirimi dağıtımının satır indirimi sonrası kalan tabanı kullandığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Use_PostLineDiscount_Base_For_Invoice_Distribution()
    {
        var service = CreateService();
        var lineDiscount = new DiscountCalculationInput(
            DiscountScope.Line,
            DiscountType.FixedLineTotal,
            50m,
            DiscountTaxBasis.ExcludingVat);
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            30m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(1, enteredUnitPrice: 100m, lineDiscount: lineDiscount),
                CreateLine(2, enteredUnitPrice: 100m)
            ],
            invoiceDiscount));

        result.Lines[0].InvoiceDiscountShareExcludingVat.Should().Be(10m);
        result.Lines[1].InvoiceDiscountShareExcludingVat.Should().Be(20m);
        result.Lines[0].TotalDiscountAmountExcludingVat.Should().Be(60m);
        result.Lines[1].TotalDiscountAmountExcludingVat.Should().Be(20m);
    }

    // Burada indirim uygunluğu kapalı satırın fatura indirimi payı almadığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Exclude_Ineligible_Lines_From_Invoice_Discount()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(1, enteredUnitPrice: 100m, isInvoiceDiscountEligible: false),
                CreateLine(2, enteredUnitPrice: 200m)
            ],
            invoiceDiscount));

        result.Lines[0].InvoiceDiscountShareExcludingVat.Should().Be(0m);
        result.Lines[1].InvoiceDiscountShareExcludingVat.Should().Be(20m);
        result.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(20m);
    }

    // Burada farklı KDV oranlarında dahil sabit indirimin her satırın kendi oranıyla ayrıştırıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Convert_Inclusive_Invoice_Discount_Per_Line_VatRate()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            23m,
            DiscountTaxBasis.IncludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(
                    1,
                    enteredUnitPrice: 120m,
                    vatRate: 20m,
                    priceEntryMode: PriceEntryMode.IncludingVat),
                CreateLine(
                    2,
                    enteredUnitPrice: 110m,
                    vatRate: 10m,
                    priceEntryMode: PriceEntryMode.IncludingVat)
            ],
            invoiceDiscount));

        result.Lines[0].InvoiceDiscountShareIncludingVat.Should().Be(12m);
        result.Lines[1].InvoiceDiscountShareIncludingVat.Should().Be(11m);
        result.Lines[0].InvoiceDiscountShareExcludingVat.Should().Be(10m);
        result.Lines[1].InvoiceDiscountShareExcludingVat.Should().Be(10m);
        result.Totals.InvoiceDiscountTotalIncludingVat.Should().Be(23m);
        result.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(20m);
        result.Totals.VatTotal.Should().Be(27m);
        result.Totals.GrandTotalIncludingVat.Should().Be(207m);
    }

    // Burada dağıtım kuruş farkının satır numarasına göre son uygun satıra verildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Assign_Rounding_Difference_To_Final_Eligible_Line()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            1m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(3, enteredUnitPrice: 100m),
                CreateLine(1, enteredUnitPrice: 100m),
                CreateLine(2, enteredUnitPrice: 100m)
            ],
            invoiceDiscount));

        result.Lines.Select(line => line.LineNumber).Should().Equal(1, 2, 3);
        result.Lines[0].InvoiceDiscountShareExcludingVat.Should().Be(0.33m);
        result.Lines[1].InvoiceDiscountShareExcludingVat.Should().Be(0.33m);
        result.Lines[2].InvoiceDiscountShareExcludingVat.Should().Be(0.34m);
        result.Lines.Sum(line => line.InvoiceDiscountShareExcludingVat).Should().Be(1m);
    }

    // Burada çok küçük hedef indirimin hiçbir satırda negatif pay oluşturmadan eksiksiz dağıtıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Distribute_Penny_Target_Without_Negative_Shares()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            0.02m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(1, enteredUnitPrice: 0.01m, vatRate: 0m),
                CreateLine(2, enteredUnitPrice: 0.01m, vatRate: 0m),
                CreateLine(3, enteredUnitPrice: 0.01m, vatRate: 0m),
                CreateLine(4, enteredUnitPrice: 0.01m, vatRate: 0m)
            ],
            invoiceDiscount));

        result.Lines.Should().OnlyContain(line => line.InvoiceDiscountShareExcludingVat >= 0m);
        result.Lines.Sum(line => line.InvoiceDiscountShareExcludingVat).Should().Be(0.02m);
        result.Totals.NetAmountExcludingVat.Should().Be(0.02m);
    }

    // Burada çok sayıda kuruşluk satırda hedef indirimin hiçbir satır kapasitesini aşmadan dağıtıldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Reconcile_Fixed_Discount_Within_Each_Line_Capacity()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            0.04m,
            DiscountTaxBasis.ExcludingVat);
        var lines = Enumerable
            .Range(1, 11)
            .Select(lineNumber => CreateLine(
                lineNumber,
                enteredUnitPrice: 0.01m,
                vatRate: 0m))
            .ToArray();

        var result = service.Calculate(new InvoiceCalculationInput(
            lines,
            invoiceDiscount));

        result.Lines.Should().OnlyContain(
            line =>
                line.InvoiceDiscountShareExcludingVat == 0m ||
                line.InvoiceDiscountShareExcludingVat == 0.01m);
        result.Lines.Sum(line => line.InvoiceDiscountShareExcludingVat).Should().Be(0.04m);
        result.Lines.Should().OnlyContain(
            line => line.InvoiceDiscountShareExcludingVat <= line.GrossAmountExcludingVat);
    }

    // Burada desteklenen büyük parasal değerlerde oransal çarpımın gereksiz decimal taşması üretmediğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Distribute_Large_Fixed_Discount_Without_Intermediate_Overflow()
    {
        var service = CreateService();
        const decimal lineAmount = 5_000_000_000_000_000m;
        const decimal discountAmount = 5_000_000_000_000_000m;
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            discountAmount,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                CreateLine(1, enteredUnitPrice: lineAmount, vatRate: 0m),
                CreateLine(2, enteredUnitPrice: lineAmount, vatRate: 0m)
            ],
            invoiceDiscount));

        result.Lines[0].InvoiceDiscountShareExcludingVat
            .Should().Be(2_500_000_000_000_000m);
        result.Lines[1].InvoiceDiscountShareExcludingVat
            .Should().Be(2_500_000_000_000_000m);
        result.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(discountAmount);
    }

    // Burada yüzde yüz fatura indiriminin bütün uygun satırları sıfıra indirebildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Allow_OneHundredPercent_Invoice_Discount()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.Percentage,
            100m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1), CreateLine(2, enteredUnitPrice: 50m)],
            invoiceDiscount));

        result.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(150m);
        result.Totals.NetAmountExcludingVat.Should().Be(0m);
        result.Totals.VatTotal.Should().Be(0m);
        result.Totals.GrandTotalIncludingVat.Should().Be(0m);
    }

    // Burada sabit fatura indiriminin uygun satır tabanını aşamadığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Invoice_Discount_Above_Eligible_Base()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            100.01m,
            DiscountTaxBasis.ExcludingVat);

        Action act = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1)],
            invoiceDiscount));

        act.Should().Throw<DomainException>();
    }

    // Burada pozitif tabanı olmayan uygun satırlarda fatura indirimi tanımının reddedildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Reject_Invoice_Discount_Without_Positive_Eligible_Base()
    {
        var service = CreateService();
        var invoiceDiscount = CreateInvoiceDiscount(
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.ExcludingVat);

        Action noEligibleLine = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, isInvoiceDiscountEligible: false)],
            invoiceDiscount));
        Action zeroBase = () => service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, enteredUnitPrice: 0m)],
            invoiceDiscount));

        noEligibleLine.Should().Throw<DomainException>();
        zeroBase.Should().Throw<DomainException>();
    }

    // Burada sıfır yüzde ve sıfır sabit fatura indiriminin uygun taban aramadan no-op kaldığını doğruluyorum.
    [Fact]
    public void Calculate_Should_Treat_Zero_Invoice_Discounts_As_NoOp()
    {
        var service = CreateService();
        var zeroPercentage = CreateInvoiceDiscount(
            DiscountType.Percentage,
            0m,
            DiscountTaxBasis.ExcludingVat);
        var zeroFixed = CreateInvoiceDiscount(
            DiscountType.FixedInvoiceTotal,
            0m,
            DiscountTaxBasis.IncludingVat);

        var percentageResult = service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, enteredUnitPrice: 0m)],
            zeroPercentage));
        var fixedResult = service.Calculate(new InvoiceCalculationInput(
            [CreateLine(1, isInvoiceDiscountEligible: false)],
            zeroFixed));

        percentageResult.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(0m);
        percentageResult.Totals.GrandTotalIncludingVat.Should().Be(0m);
        fixedResult.Totals.InvoiceDiscountTotalIncludingVat.Should().Be(0m);
        fixedResult.Totals.GrandTotalIncludingVat.Should().Be(120m);
    }

    // Burada testlerin hesap motorunu ortak yuvarlama politikasıyla kurmasını sağlıyorum.
    private static InvoiceCalculationService CreateService()
    {
        return new InvoiceCalculationService(new AccountingRoundingPolicy());
    }

    // Burada test için fatura scope'lu indirim tanımını hazırlıyorum.
    private static DiscountCalculationInput CreateInvoiceDiscount(
        DiscountType type,
        decimal value,
        DiscountTaxBasis taxBasis)
    {
        return new DiscountCalculationInput(
            DiscountScope.Invoice,
            type,
            value,
            taxBasis);
    }

    // Burada dağıtım testlerinin satır girdilerini güvenli varsayılanlarla hazırlıyorum.
    private static InvoiceLineCalculationInput CreateLine(
        int lineNumber,
        decimal enteredUnitPrice = 100m,
        decimal vatRate = 20m,
        PriceEntryMode priceEntryMode = PriceEntryMode.ExcludingVat,
        DiscountCalculationInput? lineDiscount = null,
        bool isInvoiceDiscountEligible = true)
    {
        return new InvoiceLineCalculationInput(
            lineNumber,
            1m,
            1m,
            enteredUnitPrice,
            priceEntryMode,
            vatRate,
            lineDiscount,
            isInvoiceDiscountEligible);
    }
}
