using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.PurchaseInvoices;

public sealed class PurchaseInvoiceTests
{
    // Burada taslak faturanın allocation eklenmeden hiçbir maliyet veya posting etkisi üretmediğini doğruluyorum.
    [Fact]
    public void Draft_Should_Start_Without_Allocations_Or_Posting_Effects()
    {
        var invoice = CreateInvoice();
        var line = CreateLine(invoice, 1, 5);
        invoice.AddLine(line, 1);
        ApplySimpleCalculation(invoice, line, 500m, 100m);

        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.PostedAt.Should().BeNull();
        line.Allocations.Should().BeEmpty();
        invoice.TotalFinalCostExcludingVat.Should().Be(500m);
    }

    // Burada tek fatura satırının birden fazla mevcut stok hareketinden kısmi miktar alabileceğini doğruluyorum.
    [Fact]
    public void Line_Should_Support_Multiple_Partial_Allocations()
    {
        var invoice = CreateInvoice();
        var line = CreateLine(invoice, 1, 10);
        invoice.AddLine(line, 1);

        line.AddAllocation(Guid.NewGuid(), 4);
        line.AddAllocation(Guid.NewGuid(), 6);

        line.IsFullyAllocated().Should().BeTrue();
        line.Allocations.Sum(item => item.AllocatedQuantity).Should().Be(10);
    }

    // Burada aynı hareketin aynı satıra iki kez eklenmesini ve satır miktarının aşılmasını reddediyorum.
    [Fact]
    public void Line_Should_Reject_Duplicate_And_Excessive_Allocation()
    {
        var invoice = CreateInvoice();
        var line = CreateLine(invoice, 1, 5);
        invoice.AddLine(line, 1);
        var movementId = Guid.NewGuid();

        line.AddAllocation(movementId, 3);
        var duplicate = () => line.AddAllocation(movementId, 1);
        var excessive = () => line.AddAllocation(Guid.NewGuid(), 3);

        duplicate.Should().Throw<ECommerce.Domain.Common.DomainException>();
        excessive.Should().Throw<ECommerce.Domain.Common.DomainException>();
    }

    // Burada CostLayer miktar ve maliyetinin yalnız onaylı allocation ile KDV hariç final birim maliyetten geldiğini doğruluyorum.
    // Burada ticari güncellemenin satır kimliği, ürün snapshot'ı ve mevcut allocation kayıtlarını koruyup aşırı miktar azaltımını reddettiğini doğruluyorum.
    [Fact]
    public void Commercial_Update_Should_Preserve_Identity_Snapshot_And_Allocations()
    {
        var invoice = CreateInvoice();
        var line = CreateLine(invoice, 1, 5);
        invoice.AddLine(line, 1);
        var allocation = line.AddAllocation(Guid.NewGuid(), 3);
        var originalLineId = line.Id;
        var originalVariantId = line.ProductVariantId;
        var originalSku = line.SkuSnapshot;

        line.UpdateCommercialTerms(
            6m,
            "KUTU",
            1m,
            6,
            PriceEntryMode.IncludingVat,
            75m,
            10m,
            null,
            null,
            null,
            null,
            true);
        var excessiveReduction = () => line.UpdateCommercialTerms(
            2m,
            "KUTU",
            1m,
            2,
            PriceEntryMode.ExcludingVat,
            50m,
            20m,
            null,
            null,
            null,
            null,
            true);

        line.Id.Should().Be(originalLineId);
        line.ProductVariantId.Should().Be(originalVariantId);
        line.SkuSnapshot.Should().Be(originalSku);
        line.EnteredUnitPrice.Should().Be(75m);
        line.Allocations.Should().ContainSingle(item => item.Id == allocation.Id);
        excessiveReduction.Should().Throw<ECommerce.Domain.Common.DomainException>();
        line.StockQuantity.Should().Be(6);
        line.Allocations.Sum(item => item.AllocatedQuantity).Should().Be(3);
    }

    // Burada maliyet katmanının miktarı ve maliyeti yalnız onaylı allocation ile KDV hariç final birim maliyetten aldığını doğruluyorum.
    [Fact]
    public void CostLayer_Should_Use_Allocation_Quantity_And_Final_Unit_Cost()
    {
        var invoice = CreateInvoice();
        var line = CreateLine(invoice, 1, 5);
        invoice.AddLine(line, 1);
        ApplySimpleCalculation(invoice, line, 500m, 100m);
        var allocation = line.AddAllocation(Guid.NewGuid(), 3);

        var layer = new InventoryCostLayer(line, allocation, invoice.InvoiceDate);

        layer.OriginalQuantity.Should().Be(3);
        layer.RemainingQuantity.Should().Be(3);
        layer.UnitCostExcludingVat.Should().Be(100m);
        layer.TotalCostExcludingVat.Should().Be(300m);
    }

    // Burada post edilmiş faturanın cari hesap master değişikliklerinden etkilenmeyen tarihsel snapshot'ını koruduğunu doğruluyorum.
    [Fact]
    public void Posted_Invoice_Snapshots_Should_Not_Change_When_CurrentAccount_Changes()
    {
        var account = CreateSupplierAccount();
        var invoice = new PurchaseInvoice(
            account, "INV-SNAPSHOT", new DateTime(2026, 7, 26), null, "TRY", 1m,
            null, null, null, null, 1);
        var line = CreateLine(invoice, 1, 1);
        invoice.AddLine(line, 1);
        ApplySimpleCalculation(invoice, line, 100m, 100m);
        invoice.MarkPosted(1, new DateTime(2026, 7, 27));

        account.Update(
            "SUP-1", CurrentAccountType.Supplier, "Changed Supplier", null, null,
            "9999999999", "Changed Office", "5559999999", "changed@example.com",
            "Türkiye", "Ankara", "Çankaya", "Kızılay", "Changed Address", "06000");

        invoice.CurrentAccountNameSnapshot.Should().Be("Supplier");
        invoice.TaxNumberSnapshot.Should().Be("1234567890");
        invoice.TaxOfficeSnapshot.Should().Be("Tax Office");
        invoice.PhoneNumberSnapshot.Should().Be("5550000000");
        invoice.EmailSnapshot.Should().Be("supplier@example.com");
        invoice.AddressSnapshot.Should().Contain("Test Caddesi 1");
    }

    // Burada yalnız müşteri rolündeki cari hesabın alış faturasında tedarikçi olarak kullanılamadığını doğruluyorum.
    [Fact]
    public void PurchaseInvoice_Should_Reject_Customer_Only_CurrentAccount()
    {
        var customer = new CurrentAccount(
            "CUS-1", CurrentAccountType.Customer, "Customer", null, null, null, null,
            null, null, null, null, null, null, null, null);

        var action = () => new PurchaseInvoice(
            customer, "INV-1", new DateTime(2026, 7, 26), null, "TRY", 1m,
            null, null, null, null, 1);

        action.Should().Throw<ECommerce.Domain.Common.DomainException>();
    }

    // Burada domain sınırının TRY dışındaki para birimini veya birim olmayan kuru doğrudan kullanımda da reddettiğini doğruluyorum.
    [Theory]
    [InlineData("USD", 1)]
    [InlineData("TRY", 2)]
    public void PurchaseInvoice_Should_Require_Try_And_Unit_Exchange_Rate(
        string currencyCode,
        int exchangeRate)
    {
        var action = () => new PurchaseInvoice(
            CreateSupplierAccount(),
            "INV-CURRENCY",
            new DateTime(2026, 7, 26),
            null,
            currencyCode,
            exchangeRate,
            null,
            null,
            null,
            null,
            1);

        action.Should().Throw<ECommerce.Domain.Common.DomainException>();
    }

    // Burada testler için geçerli taslak alış faturası oluşturuyorum.
    private static PurchaseInvoice CreateInvoice()
    {
        return new PurchaseInvoice(
            CreateSupplierAccount(),
            "INV-1",
            new DateTime(2026, 7, 26),
            null,
            "TRY",
            1m,
            null,
            null,
            null,
            null,
            1);
    }

    // Burada alış faturası domain testleri için etkin bir tedarikçi cari hesabı hazırlıyorum.
    private static CurrentAccount CreateSupplierAccount()
    {
        return new CurrentAccount(
            "SUP-1", CurrentAccountType.Supplier, "Supplier", null, null, "1234567890",
            "Tax Office", "5550000000", "supplier@example.com", "Türkiye", "İstanbul",
            "Kadıköy", "Caferağa", "Test Caddesi 1", "34710");
    }

    // Burada testler için güvenilir snapshot taşıyan alış faturası satırı oluşturuyorum.
    private static PurchaseInvoiceLine CreateLine(PurchaseInvoice invoice, int lineNumber, int quantity)
    {
        return new PurchaseInvoiceLine(
            invoice,
            lineNumber,
            1,
            Guid.NewGuid(),
            "Product",
            "Variant",
            $"SKU-{lineNumber}",
            null,
            quantity,
            "ADET",
            1m,
            quantity,
            PriceEntryMode.ExcludingVat,
            100m,
            20m,
            null,
            null,
            null,
            null,
            true);
    }

    // Burada test satırına basit KDV hariç hesap sonucunu uyguluyorum.
    private static void ApplySimpleCalculation(
        PurchaseInvoice invoice,
        PurchaseInvoiceLine line,
        decimal netAmount,
        decimal unitCost)
    {
        line.ApplyCalculation(
            unitCost,
            unitCost * 1.2m,
            netAmount,
            netAmount * 1.2m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            netAmount,
            netAmount * 0.2m,
            netAmount * 1.2m);
        invoice.ApplyTotals(
            netAmount,
            netAmount * 1.2m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            netAmount,
            netAmount * 0.2m,
            netAmount * 1.2m);
    }
}
