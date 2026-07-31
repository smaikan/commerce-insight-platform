using ECommerce.Application.Accounting.Common.Calculations;
using ECommerce.Domain.Accounting.Common.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.Calculations;

public sealed class InvoiceRoundingTests
{
    // Burada karşı birim fiyatın dört, satır tutarlarının iki ondalıkla üretildiğini doğruluyorum.
    [Fact]
    public void Calculate_Should_Use_Centralized_Unit_And_Money_Precision()
    {
        var service = CreateService();

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                new InvoiceLineCalculationInput(
                    1,
                    3m,
                    1m,
                    10.1234m,
                    PriceEntryMode.ExcludingVat,
                    20m)
            ]));

        var line = result.Lines.Single();
        line.UnitPriceExcludingVat.Should().Be(10.1234m);
        line.UnitPriceIncludingVat.Should().Be(12.1481m);
        line.GrossAmountExcludingVat.Should().Be(30.37m);
        line.GrossAmountIncludingVat.Should().Be(36.44m);
        line.VatAmount.Should().Be(6.07m);
    }

    // Burada satır indirimleri ile fatura paylarının toplam indirimi doğru oluşturduğunu doğruluyorum.
    [Fact]
    public void Calculate_Should_Combine_Line_And_Invoice_Discounts()
    {
        var service = CreateService();
        var lineDiscount = new DiscountCalculationInput(
            DiscountScope.Line,
            DiscountType.FixedLineTotal,
            10m,
            DiscountTaxBasis.ExcludingVat);
        var invoiceDiscount = new DiscountCalculationInput(
            DiscountScope.Invoice,
            DiscountType.FixedInvoiceTotal,
            15m,
            DiscountTaxBasis.ExcludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                new InvoiceLineCalculationInput(
                    1,
                    1m,
                    1m,
                    100m,
                    PriceEntryMode.ExcludingVat,
                    20m,
                    lineDiscount),
                new InvoiceLineCalculationInput(
                    2,
                    1m,
                    1m,
                    50m,
                    PriceEntryMode.ExcludingVat,
                    10m)
            ],
            invoiceDiscount));

        result.Totals.LineDiscountTotalExcludingVat.Should().Be(10m);
        result.Totals.InvoiceDiscountTotalExcludingVat.Should().Be(15m);
        result.Totals.TotalDiscountExcludingVat.Should().Be(25m);
        result.Lines.Sum(line => line.TotalDiscountAmountExcludingVat)
            .Should().Be(result.Totals.TotalDiscountExcludingVat);
    }

    // Burada bütün başlık toplamlarının yalnız satır sonuçlarının birebir toplamından oluştuğunu doğruluyorum.
    [Fact]
    public void Calculate_Should_Keep_Line_And_Header_Totals_Exactly_Equal()
    {
        var service = CreateService();
        var invoiceDiscount = new DiscountCalculationInput(
            DiscountScope.Invoice,
            DiscountType.FixedInvoiceTotal,
            1m,
            DiscountTaxBasis.IncludingVat);

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                new InvoiceLineCalculationInput(
                    1,
                    1m,
                    1m,
                    33.33m,
                    PriceEntryMode.ExcludingVat,
                    20m),
                new InvoiceLineCalculationInput(
                    2,
                    1m,
                    1m,
                    66.67m,
                    PriceEntryMode.ExcludingVat,
                    10m),
                new InvoiceLineCalculationInput(
                    3,
                    1m,
                    1m,
                    25m,
                    PriceEntryMode.ExcludingVat,
                    1m)
            ],
            invoiceDiscount));

        result.Totals.SubtotalExcludingVat
            .Should().Be(result.Lines.Sum(line => line.GrossAmountExcludingVat));
        result.Totals.SubtotalIncludingVat
            .Should().Be(result.Lines.Sum(line => line.GrossAmountIncludingVat));
        result.Totals.LineDiscountTotalExcludingVat
            .Should().Be(result.Lines.Sum(line => line.LineDiscountAmountExcludingVat));
        result.Totals.InvoiceDiscountTotalExcludingVat
            .Should().Be(result.Lines.Sum(line => line.InvoiceDiscountShareExcludingVat));
        result.Totals.TotalDiscountExcludingVat
            .Should().Be(result.Lines.Sum(line => line.TotalDiscountAmountExcludingVat));
        result.Totals.NetAmountExcludingVat
            .Should().Be(result.Lines.Sum(line => line.NetAmountExcludingVat));
        result.Totals.VatTotal
            .Should().Be(result.Lines.Sum(line => line.VatAmount));
        result.Totals.GrandTotalIncludingVat
            .Should().Be(result.Lines.Sum(line => line.TotalAmountIncludingVat));
        result.Totals.GrandTotalIncludingVat
            .Should().Be(result.Totals.NetAmountExcludingVat + result.Totals.VatTotal);
    }

    // Burada sonuç satırlarının dışarıdan değiştirilemeyen salt okunur koleksiyonla döndüğünü doğruluyorum.
    [Fact]
    public void Calculate_Should_Return_ReadOnly_Line_Results()
    {
        var service = CreateService();

        var result = service.Calculate(new InvoiceCalculationInput(
            [
                new InvoiceLineCalculationInput(
                    1,
                    1m,
                    1m,
                    100m,
                    PriceEntryMode.ExcludingVat,
                    20m)
            ]));

        var list = result.Lines as IList<InvoiceLineCalculationResult>;
        list.Should().NotBeNull();
        list!.IsReadOnly.Should().BeTrue();

        Action mutate = () => list.Add(result.Lines[0]);

        mutate.Should().Throw<NotSupportedException>();
    }

    // Burada input modelinin çağıranın sonradan değiştirdiği kaynak listeden etkilenmediğini doğruluyorum.
    [Fact]
    public void InvoiceCalculationInput_Should_Defensively_Copy_Source_Lines()
    {
        var sourceLines = new List<InvoiceLineCalculationInput>
        {
            new(
                1,
                1m,
                1m,
                100m,
                PriceEntryMode.ExcludingVat,
                20m)
        };
        var input = new InvoiceCalculationInput(sourceLines);

        sourceLines.Add(new InvoiceLineCalculationInput(
            2,
            1m,
            1m,
            50m,
            PriceEntryMode.ExcludingVat,
            10m));

        input.Lines.Should().ContainSingle();
    }

    // Burada testlerin hesap motorunu ortak yuvarlama politikasıyla kurmasını sağlıyorum.
    private static InvoiceCalculationService CreateService()
    {
        return new InvoiceCalculationService(new AccountingRoundingPolicy());
    }
}
