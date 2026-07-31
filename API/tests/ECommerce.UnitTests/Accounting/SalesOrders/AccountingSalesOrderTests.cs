using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.PurchaseInvoices;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.SalesOrders;

public sealed class AccountingSalesOrderTests
{
    // Burada taslak satış siparişinin stok, FIFO, cari alacak veya fatura etkisi üretmediğini doğruluyorum.
    [Fact]
    public void Draft_Should_Not_Create_Stock_Fifo_Receivable_Or_Invoice_Effects()
    {
        var account = CreateCustomerAccount();
        var order = CreateOrder(account);
        var item = CreateSalesItem(order, Guid.NewGuid(), 1, 2, 200m);
        order.AddItem(item, 1);
        ApplyOrderTotals(order);

        order.Status.Should().Be(InvoiceStatus.Draft);
        order.PostedAt.Should().BeNull();
        order.SalesInvoice.Should().BeNull();
        item.StockMovements.Should().BeEmpty();
        item.CostLayerConsumptions.Should().BeEmpty();
        account.Transactions.Should().BeEmpty();
        order.TotalCostOfGoodsSold.Should().Be(0m);
    }

    // Burada ProductVariantId ve ürün snapshot değerlerinin doğrudan Accounting satır girdisinden korunduğunu doğruluyorum.
    [Fact]
    public void Item_Should_Keep_Request_Variant_And_Trusted_Snapshots()
    {
        var order = CreateOrder(CreateCustomerAccount());
        var variantId = Guid.NewGuid();

        var item = new AccountingSalesOrderItem(
            order,
            7,
            42,
            variantId,
            "Accounting Product",
            "Blue / Large",
            "SKU-ACCOUNTING-1",
            "8690000000001",
            3m,
            "ADET",
            1m,
            3,
            PriceEntryMode.ExcludingVat,
            125m,
            20m,
            null,
            null,
            null,
            null,
            true);

        item.ProductVariantId.Should().Be(variantId);
        item.ProductId.Should().Be(42);
        item.ProductNameSnapshot.Should().Be("Accounting Product");
        item.VariantNameSnapshot.Should().Be("Blue / Large");
        item.SkuSnapshot.Should().Be("SKU-ACCOUNTING-1");
        item.StockQuantity.Should().Be(3);
    }

    // Burada satır domain'inin UnitBasis alanını yalnız birim başına indirim türünde kabul ettiğini doğruluyorum.
    [Fact]
    public void Item_Should_Reject_Unit_Basis_Outside_Fixed_Per_Unit_Discount()
    {
        var order = CreateOrder(CreateCustomerAccount());

        var action = () => new AccountingSalesOrderItem(
            order,
            1,
            42,
            Guid.NewGuid(),
            "Accounting Product",
            "Default",
            "SKU-DISCOUNT",
            null,
            1m,
            "ADET",
            1m,
            1,
            PriceEntryMode.ExcludingVat,
            100m,
            20m,
            DiscountType.Percentage,
            10m,
            DiscountTaxBasis.ExcludingVat,
            DiscountUnitBasis.SaleUnit,
            true);

        action.Should().Throw<DomainException>();
    }

    // Burada açık maliyet katmanlarının CostDate, CreatedAt ve Id ile kararlı FIFO sırasına girdiğini doğruluyorum.
    [Fact]
    public void CostLayers_Should_Use_Deterministic_Fifo_Ordering()
    {
        var variantId = Guid.NewGuid();
        var sameCostDate = new DateTime(2026, 1, 1);
        var sameCreatedAt = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var firstById = CreateLayer(variantId, 1, 100m, sameCostDate);
        var secondById = CreateLayer(variantId, 1, 100m, sameCostDate);
        var laterByDate = CreateLayer(variantId, 1, 100m, new DateTime(2026, 2, 1));
        SetLayerOrdering(firstById, sameCostDate, sameCreatedAt, lowerId);
        SetLayerOrdering(secondById, sameCostDate, sameCreatedAt, higherId);
        SetLayerOrdering(
            laterByDate,
            new DateTime(2026, 2, 1),
            sameCreatedAt.AddDays(-10),
            Guid.Empty);

        var ordered = InventoryCostLayer.OrderForFifo([
            laterByDate,
            secondById,
            firstById
        ]);

        ordered.Should().ContainInOrder(firstById, secondById, laterByDate);
    }

    // Burada tek satış satırının birden çok FIFO katmanından gerçek maliyet tüketip doğru kâr üretmesini doğruluyorum.
    [Fact]
    public void One_Item_Should_Consume_Multiple_Layers_And_Calculate_Profitability()
    {
        var scenario = CreatePostedOrderWithFifo();

        scenario.Item.CostLayerConsumptions.Should().HaveCount(2);
        scenario.Item.CostOfGoodsSold.Should().Be(920m);
        scenario.Item.GrossProfitExcludingVat.Should().Be(680m);
        scenario.Item.GrossProfitMargin.Should().Be(42.5m);
        scenario.Order.TotalCostOfGoodsSold.Should().Be(920m);
        scenario.Order.GrossProfitExcludingVat.Should().Be(680m);
        scenario.Order.GrossProfitMargin.Should().Be(42.5m);
        scenario.FirstLayer.RemainingQuantity.Should().Be(0);
        scenario.FirstLayer.Status.Should().Be(CostLayerStatus.Consumed);
        scenario.SecondLayer.RemainingQuantity.Should().Be(12);
        scenario.SecondLayer.Status.Should().Be(CostLayerStatus.Open);
    }

    // Burada maliyet katmanının kalan miktarından fazlasını tüketmeyi reddedip bakiyeyi değiştirmediğini doğruluyorum.
    [Fact]
    public void CostLayer_Should_Prevent_Remaining_Quantity_From_Becoming_Negative()
    {
        var order = CreateOrder(CreateCustomerAccount());
        var variant = CreateVariant(10);
        var item = CreateSalesItem(order, variant.Id, 1, 3, 200m);
        order.AddItem(item, 1);
        ApplyOrderTotals(order);
        var movement = variant.ApplyStockMovement(
            -3,
            StockMovementType.AccountingSale,
            "Accounting unit test sale.");
        item.LinkStockMovement(movement);
        var layer = CreateLayer(variant.Id, 2, 100m, new DateTime(2026, 1, 1));
        var originalToken = layer.ConcurrencyToken;

        var action = () => layer.Consume(item, movement, 3);

        action.Should().Throw<DomainException>();
        layer.RemainingQuantity.Should().Be(2);
        layer.ConcurrencyToken.Should().Be(originalToken);
        item.CostLayerConsumptions.Should().BeEmpty();
    }

    // Burada Accounting satış satırının başka bir negatif hareket türünü kendi stok çıkışı gibi eşleyemediğini doğruluyorum.
    [Fact]
    public void Sales_Item_Should_Link_Only_AccountingSale_Stock_Movements()
    {
        var order = CreateOrder(CreateCustomerAccount());
        var variant = CreateVariant(3);
        var item = CreateSalesItem(order, variant.Id, 1, 1, 100m);
        order.AddItem(item, 1);
        ApplyOrderTotals(order);
        var lossMovement = variant.ApplyStockMovement(
            -1,
            StockMovementType.Loss,
            "Damaged before accounting sale.");

        var action = () => item.LinkStockMovement(lossMovement);

        action.Should().Throw<DomainException>();
        item.StockMovements.Should().BeEmpty();
    }

    // Burada faturanın isteğe bağlı kaldığını, tekilliği koruduğunu ve taslak sipariş değişiklikleriyle senkronlandığını doğruluyorum.
    [Fact]
    public void Optional_Invoice_Should_Attach_Once_And_Sync_From_Draft_Order()
    {
        var account = CreateCustomerAccount();
        var order = CreateOrder(account, shippingTotal: 25m);
        var firstItem = CreateSalesItem(order, Guid.NewGuid(), 1, 1, 200m);
        order.AddItem(firstItem, 1);
        ApplyOrderTotals(order);
        order.SalesInvoice.Should().BeNull();
        var invoice = new SalesInvoice(
            order,
            "SINV-1",
            new DateTime(2026, 7, 26),
            new DateTime(2026, 8, 26),
            "Internal invoice",
            1);
        var replacement = CreateSalesItem(order, Guid.NewGuid(), 1, 2, 300m);
        order.ReplaceItem(firstItem.Id, replacement, 1);
        ApplyOrderTotals(order);

        invoice.SyncFromOrder(order, 1);
        var secondInvoice = () => new SalesInvoice(
            order,
            "SINV-2",
            new DateTime(2026, 7, 27),
            null,
            null,
            1);

        order.SalesInvoice.Should().BeSameAs(invoice);
        invoice.Lines.Should().ContainSingle();
        invoice.Lines.Single().AccountingSalesOrderItemId.Should().Be(replacement.Id);
        invoice.Lines.Single().Quantity.Should().Be(2m);
        invoice.GrandTotalIncludingVat.Should().Be(order.GrandTotalIncludingVat);
        invoice.ShippingTotal.Should().Be(25m);
        secondInvoice.Should().Throw<DomainException>();
    }

    // Burada post edilmiş siparişe sonradan fatura eklemenin stok, FIFO veya cari etkileri tekrarlamadığını doğruluyorum.
    [Fact]
    public void Posted_Order_Should_Allow_One_Later_Invoice_Without_Repeating_Effects()
    {
        var scenario = CreatePostedOrderWithFifo();
        var movementCount = scenario.Item.StockMovements.Count;
        var consumptionCount = scenario.Item.CostLayerConsumptions.Count;
        scenario.Order.SalesInvoice.Should().BeNull();

        var invoice = new SalesInvoice(
            scenario.Order,
            "SINV-LATER",
            new DateTime(2026, 7, 27),
            null,
            null,
            1);
        invoice.MarkPosted(1, new DateTime(2026, 7, 27, 12, 0, 0, DateTimeKind.Utc));

        invoice.Status.Should().Be(InvoiceStatus.Posted);
        invoice.TotalCostOfGoodsSold.Should().Be(920m);
        invoice.GrossProfitExcludingVat.Should().Be(680m);
        invoice.GrandTotalIncludingVat.Should().Be(scenario.Order.GrandTotalIncludingVat);
        scenario.Item.StockMovements.Should().HaveCount(movementCount);
        scenario.Item.CostLayerConsumptions.Should().HaveCount(consumptionCount);
        scenario.Order.CurrentAccount.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void Fifo_Reversal_Should_Preserve_Original_Consumptions_And_Restore_Layers()
    {
        var scenario = CreatePostedOrderWithFifo();
        var originalConsumptions = scenario.Item.CostLayerConsumptions.ToArray();
        var retroactiveRewrite = () => scenario.FirstLayer.InvalidateUnconsumedPurchaseLayer();
        var reverse = scenario.Movement.ProductVariant.ApplyStockMovement(
            8, StockMovementType.AccountingSaleCancellation, "Cancelled.");

        var first = scenario.FirstLayer.Restore(originalConsumptions[0], reverse,
            scenario.Order.Id, 1, DateTime.UtcNow, "Cancelled.");
        var second = scenario.SecondLayer.Restore(originalConsumptions[1], reverse,
            scenario.Order.Id, 1, DateTime.UtcNow, "Cancelled.");

        scenario.Item.CostLayerConsumptions.Should().Equal(originalConsumptions);
        retroactiveRewrite.Should().Throw<DomainException>();
        scenario.FirstLayer.RemainingQuantity.Should().Be(5);
        scenario.SecondLayer.RemainingQuantity.Should().Be(15);
        first.CostLayerConsumptionId.Should().Be(originalConsumptions[0].Id);
        second.CostLayerConsumptionId.Should().Be(originalConsumptions[1].Id);
    }

    [Fact]
    public void Order_Cancellation_Should_Cancel_Linked_Invoice_Without_Invoice_Owning_Stock()
    {
        var scenario = CreatePostedOrderWithFifo();
        var invoice = new SalesInvoice(scenario.Order, "SINV-CANCEL", DateTime.UtcNow, null, null, 1);
        invoice.MarkPosted(1, DateTime.UtcNow);

        scenario.Order.MarkCancelled(1, DateTime.UtcNow, "Approved cancellation.");

        scenario.Order.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.Status.Should().Be(InvoiceStatus.Cancelled);
        invoice.CancellationReason.Should().Be("Approved cancellation.");
        scenario.Item.StockMovements.Should().ContainSingle(x => x.StockMovementId == scenario.Movement.Id);
    }

    [Fact]
    public void Invoice_Only_Cancellation_Should_Be_Rejected_While_Order_Is_Valid()
    {
        var scenario = CreatePostedOrderWithFifo();
        var invoice = new SalesInvoice(scenario.Order, "SINV-VALID", DateTime.UtcNow, null, null, 1);
        invoice.MarkPosted(1, DateTime.UtcNow);

        var action = () => invoice.MarkCancelledFromOrder(1, DateTime.UtcNow, "Invoice only.");

        action.Should().Throw<DomainException>();
        scenario.Order.Status.Should().Be(InvoiceStatus.Posted);
        invoice.Status.Should().Be(InvoiceStatus.Posted);
    }

    // Burada testler için aktif müşteri rolündeki tek cari hesap master kaydını oluşturuyorum.
    private static CurrentAccount CreateCustomerAccount()
    {
        return new CurrentAccount(
            $"CUS-{Guid.NewGuid():N}",
            CurrentAccountType.Customer,
            "Accounting Customer",
            null,
            null,
            "1234567890",
            "Customer Tax Office",
            "5550000000",
            "customer@example.com",
            "Türkiye",
            "İstanbul",
            "Kadıköy",
            "Caferağa",
            "Test Caddesi 1",
            "34710");
    }

    // Burada testler için stok, sepet ve kullanıcı bağı olmayan taslak muhasebe satış siparişini oluşturuyorum.
    private static AccountingSalesOrder CreateOrder(
        CurrentAccount account,
        decimal shippingTotal = 0m)
    {
        return new AccountingSalesOrder(
            account,
            $"IDEMP-{Guid.NewGuid():N}",
            $"ASO-{Guid.NewGuid():N}",
            new DateTime(2026, 7, 26),
            new DateTime(2026, 8, 26),
            "TRY",
            1m,
            null,
            null,
            null,
            shippingTotal,
            shippingTotal > 0m ? ShippingPayer.Customer : ShippingPayer.None,
            null,
            1);
    }

    // Burada testler için doğrudan Accounting girdisi ve güvenilir snapshot taşıyan satış satırını hazırlıyorum.
    private static AccountingSalesOrderItem CreateSalesItem(
        AccountingSalesOrder order,
        Guid variantId,
        int lineNumber,
        int quantity,
        decimal unitPriceExcludingVat)
    {
        var item = new AccountingSalesOrderItem(
            order,
            lineNumber,
            1,
            variantId,
            "Accounting Product",
            "Default",
            $"SKU-{variantId:N}",
            null,
            quantity,
            "ADET",
            1m,
            quantity,
            PriceEntryMode.ExcludingVat,
            unitPriceExcludingVat,
            20m,
            null,
            null,
            null,
            null,
            true);
        ApplySalesCalculation(item, quantity, unitPriceExcludingVat);
        return item;
    }

    // Burada test satış satırına indirimsiz ve yüzde yirmi KDV'li güvenilir hesap sonucunu uyguluyorum.
    private static void ApplySalesCalculation(
        AccountingSalesOrderItem item,
        int quantity,
        decimal unitPriceExcludingVat)
    {
        var grossExcludingVat = unitPriceExcludingVat * quantity;
        var vat = grossExcludingVat * 0.20m;
        var grossIncludingVat = grossExcludingVat + vat;
        item.ApplyCalculation(
            unitPriceExcludingVat,
            unitPriceExcludingVat * 1.20m,
            grossExcludingVat,
            grossIncludingVat,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            grossExcludingVat,
            vat,
            grossIncludingVat);
    }

    // Burada test siparişinin başlık toplamlarını mevcut hesaplanmış satırların tam toplamından üretiyorum.
    private static void ApplyOrderTotals(AccountingSalesOrder order)
    {
        order.ApplyTotals(
            order.Items.Sum(item => item.GrossAmountExcludingVat),
            order.Items.Sum(item => item.GrossAmountIncludingVat),
            order.Items.Sum(item => item.LineDiscountAmountExcludingVat),
            order.Items.Sum(item => item.LineDiscountAmountIncludingVat),
            order.Items.Sum(item => item.InvoiceDiscountShareExcludingVat),
            order.Items.Sum(item => item.InvoiceDiscountShareIncludingVat),
            order.Items.Sum(item => item.TotalDiscountAmountExcludingVat),
            order.Items.Sum(item => item.TotalDiscountAmountIncludingVat),
            order.Items.Sum(item => item.NetAmountExcludingVat),
            order.Items.Sum(item => item.VatAmount),
            order.Items.Sum(item => item.TotalAmountIncludingVat));
    }

    // Burada test FIFO katmanını mevcut Purchase allocation maliyetinden üretiyorum.
    private static InventoryCostLayer CreateLayer(
        Guid variantId,
        int quantity,
        decimal unitCostExcludingVat,
        DateTime costDate)
    {
        var supplier = new CurrentAccount(
            $"SUP-{Guid.NewGuid():N}",
            CurrentAccountType.Supplier,
            "Accounting Supplier",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);
        var invoice = new PurchaseInvoice(
            supplier,
            $"PINV-{Guid.NewGuid():N}",
            costDate,
            null,
            "TRY",
            1m,
            null,
            null,
            null,
            null,
            1);
        var line = new PurchaseInvoiceLine(
            invoice,
            1,
            1,
            variantId,
            "Accounting Product",
            "Default",
            $"SKU-{variantId:N}",
            null,
            quantity,
            "ADET",
            1m,
            quantity,
            PriceEntryMode.ExcludingVat,
            unitCostExcludingVat,
            20m,
            null,
            null,
            null,
            null,
            true);
        invoice.AddLine(line, 1);
        var totalExcludingVat = unitCostExcludingVat * quantity;
        var vat = totalExcludingVat * 0.20m;
        var totalIncludingVat = totalExcludingVat + vat;
        line.ApplyCalculation(
            unitCostExcludingVat,
            unitCostExcludingVat * 1.20m,
            totalExcludingVat,
            totalIncludingVat,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            totalExcludingVat,
            vat,
            totalIncludingVat);
        invoice.ApplyTotals(
            totalExcludingVat,
            totalIncludingVat,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            totalExcludingVat,
            vat,
            totalIncludingVat);
        var allocation = line.AddAllocation(Guid.NewGuid(), quantity);
        return new InventoryCostLayer(line, allocation, costDate);
    }

    // Burada FIFO sıralama anahtarlarını test senaryosu için belirli değerlere ayarlıyorum.
    private static void SetLayerOrdering(
        InventoryCostLayer layer,
        DateTime costDate,
        DateTime createdAt,
        Guid id)
    {
        typeof(InventoryCostLayer)
            .GetProperty(nameof(InventoryCostLayer.CostDate))!
            .SetValue(layer, costDate);
        typeof(InventoryCostLayer)
            .GetProperty(nameof(InventoryCostLayer.CreatedAt))!
            .SetValue(layer, createdAt);
        typeof(InventoryCostLayer)
            .GetProperty(nameof(InventoryCostLayer.Id))!
            .SetValue(layer, id);
    }

    // Burada stok hareketi üretmek için yeterli başlangıç stoğu olan mevcut ProductVariant'ı hazırlıyorum.
    private static ProductVariant CreateVariant(int stock)
    {
        return new ProductVariant(
            1,
            "Default",
            $"SKU-{Guid.NewGuid():N}",
            200m,
            stock,
            netPrice: 200m);
    }

    // Burada çok katmanlı FIFO, maliyet ve post davranışını kullanan ortak satış senaryosunu kuruyorum.
    private static PostedSalesScenario CreatePostedOrderWithFifo()
    {
        var order = CreateOrder(CreateCustomerAccount());
        var variant = CreateVariant(20);
        var item = CreateSalesItem(order, variant.Id, 1, 8, 200m);
        order.AddItem(item, 1);
        ApplyOrderTotals(order);
        var movement = variant.ApplyStockMovement(
            -8,
            StockMovementType.AccountingSale,
            "Accounting unit test sale.");
        item.LinkStockMovement(movement);
        var firstLayer = CreateLayer(variant.Id, 5, 100m, new DateTime(2026, 1, 1));
        var secondLayer = CreateLayer(variant.Id, 15, 140m, new DateTime(2026, 2, 1));
        var firstToken = firstLayer.ConcurrencyToken;
        var secondToken = secondLayer.ConcurrencyToken;
        firstLayer.Consume(item, movement, 5);
        secondLayer.Consume(item, movement, 3);
        firstLayer.ConcurrencyToken.Should().NotBe(firstToken);
        secondLayer.ConcurrencyToken.Should().NotBe(secondToken);
        order.MarkPosted(1, new DateTime(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc));
        return new PostedSalesScenario(
            order,
            item,
            movement,
            firstLayer,
            secondLayer);
    }

    // Burada ortak post senaryosunun aggregate, hareket ve maliyet katmanlarını birlikte taşıyorum.
    private sealed record PostedSalesScenario(
        AccountingSalesOrder Order,
        AccountingSalesOrderItem Item,
        StockMovement Movement,
        InventoryCostLayer FirstLayer,
        InventoryCostLayer SecondLayer);
}
