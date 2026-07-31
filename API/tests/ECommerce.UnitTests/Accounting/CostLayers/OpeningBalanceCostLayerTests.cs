using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CostLayers;
using ECommerce.Domain.Accounting.CurrentAccounts;
using ECommerce.Domain.Accounting.SalesOrders;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Accounting.CostLayers;

public sealed class OpeningBalanceCostLayerTests
{
    // Burada OpeningBalance katmanının allocation olmadan sıfır maliyetle ve gerçek açılış miktarıyla oluştuğunu doğruluyorum.
    [Fact]
    public void Constructor_Should_Create_Zero_Cost_Layer_From_Opening_Movement()
    {
        var variant = CreateVariant(5);
        var movement = variant.StockMovements.Single();

        var layer = new InventoryCostLayer(variant, movement);

        layer.SourceType.Should().Be(
            InventoryCostLayerSourceType.OpeningBalance);
        layer.StockMovementId.Should().Be(movement.Id);
        layer.ProductVariantId.Should().Be(variant.Id);
        layer.PurchaseInvoiceLineId.Should().BeNull();
        layer.PurchaseInvoiceStockAllocationId.Should().BeNull();
        layer.OriginalQuantity.Should().Be(5);
        layer.RemainingQuantity.Should().Be(5);
        layer.UnitCostExcludingVat.Should().Be(0m);
        layer.UnitCostIncludingVat.Should().Be(0m);
        layer.CostDate.Should().Be(movement.CreatedAt);
    }

    // Burada ilk oluşturma maliyetini yuvarlayıp KDV dahil değer yoksa KDV hariç değerle tamamladığını doğruluyorum.
    [Fact]
    public void Constructor_Should_Create_Opening_Layer_With_Optional_Cost()
    {
        var variant = CreateVariant(3);
        var movement = variant.StockMovements.Single();

        var layer = new InventoryCostLayer(
            variant,
            movement,
            12.34567m);

        layer.UnitCostExcludingVat.Should().Be(12.3457m);
        layer.UnitCostIncludingVat.Should().Be(12.3457m);
        layer.TotalCostExcludingVat.Should().Be(37.04m);
        layer.TotalCostIncludingVat.Should().Be(37.04m);
    }

    // Burada sonradan girilen açılış maliyetinin yalnız kalan miktarın gelecekteki tüketimine uygulandığını ve eski COGS snapshot'ını değiştirmediğini doğruluyorum.
    [Fact]
    public void Revalue_Should_Keep_Previous_Consumption_At_Zero_And_Cost_Only_Remaining_Units()
    {
        var variant = CreateVariant(5);
        var layer = new InventoryCostLayer(
            variant,
            variant.StockMovements.Single());
        var firstItem = CreateSalesItem(variant.Id, 2, 1);
        var firstMovement = variant.ApplyStockMovement(
            -2,
            StockMovementType.AccountingSale,
            "First accounting sale");
        firstItem.LinkStockMovement(firstMovement);
        var firstConsumption = layer.Consume(
            firstItem,
            firstMovement,
            2);
        var expectedToken = layer.ConcurrencyToken;

        layer.UpdateOpeningBalanceRemainingCost(10m, 12m, expectedToken);

        var secondItem = CreateSalesItem(variant.Id, 1, 2);
        var secondMovement = variant.ApplyStockMovement(
            -1,
            StockMovementType.AccountingSale,
            "Second accounting sale");
        secondItem.LinkStockMovement(secondMovement);
        var secondConsumption = layer.Consume(
            secondItem,
            secondMovement,
            1);

        firstConsumption.UnitCostExcludingVat.Should().Be(0m);
        firstConsumption.TotalCostExcludingVat.Should().Be(0m);
        secondConsumption.UnitCostExcludingVat.Should().Be(10m);
        secondConsumption.TotalCostExcludingVat.Should().Be(10m);
        layer.RemainingQuantity.Should().Be(2);
    }

    // Burada test için gerçek OpeningBalance hareketi taşıyan pozitif stoklu varyantı hazırlıyorum.
    private static ProductVariant CreateVariant(int stock)
    {
        return new ProductVariant(
            1,
            "Default",
            $"OPEN-{Guid.NewGuid():N}",
            100m,
            stock,
            netPrice: 100m);
    }

    // Burada test tüketimi için aynı varyanta bağlı taslak Accounting satış satırını hazırlıyorum.
    private static AccountingSalesOrderItem CreateSalesItem(
        Guid productVariantId,
        int stockQuantity,
        int lineNumber)
    {
        var account = new CurrentAccount(
            $"CUS-{Guid.NewGuid():N}",
            CurrentAccountType.Customer,
            "Opening Cost Customer",
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
        var order = new AccountingSalesOrder(
            account,
            $"OPEN-{Guid.NewGuid():N}",
            $"ORDER-{Guid.NewGuid():N}",
            new DateTime(2026, 7, 26),
            null,
            "TRY",
            1m,
            null,
            null,
            null,
            0m,
            ShippingPayer.None,
            null,
            1);
        var item = new AccountingSalesOrderItem(
            order,
            lineNumber,
            1,
            productVariantId,
            "Opening Cost Product",
            "Default",
            $"OPEN-{Guid.NewGuid():N}",
            null,
            stockQuantity,
            "ADET",
            1m,
            stockQuantity,
            PriceEntryMode.ExcludingVat,
            0m,
            0m,
            null,
            null,
            null,
            null,
            true);
        order.AddItem(item, 1);
        return item;
    }
}
