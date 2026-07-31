using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class StockMovementTests
{
    // Burada pozitif başlangıç stokunun açılış bakiyesi olarak imzalı giriş hareketi ürettiğini doğruluyorum.
    [Fact]
    public void Constructor_Should_Create_Opening_Balance_For_A_Positive_Initial_Stock()
    {
        var variant = CreateVariant(stock: 8);

        var movement = variant.StockMovements.Single();

        variant.Stock.Should().Be(8);
        movement.Type.Should().Be(StockMovementType.OpeningBalance);
        movement.Direction.Should().Be(StockMovementDirection.In);
        movement.QuantityDelta.Should().Be(8);
        movement.StockBeforeMovement.Should().Be(0);
        movement.StockAfterMovement.Should().Be(8);
    }

    // Burada sipariş satışının sipariş kimliğiyle bağlı, negatif imzalı çıkış hareketi olarak yazıldığını doğruluyorum.
    [Fact]
    public void ApplyStockMovement_Should_Record_A_Sale_As_An_Outgoing_Signed_Delta()
    {
        var variant = CreateVariant(stock: 8);
        var orderId = Guid.NewGuid();

        var movement = variant.ApplyStockMovement(
            -2,
            StockMovementType.Sale,
            "Order created.",
            orderId);

        variant.Stock.Should().Be(6);
        movement.Type.Should().Be(StockMovementType.Sale);
        movement.Direction.Should().Be(StockMovementDirection.Out);
        movement.QuantityDelta.Should().Be(-2);
        movement.StockBeforeMovement.Should().Be(8);
        movement.StockAfterMovement.Should().Be(6);
        movement.OrderId.Should().Be(orderId);
    }

    // Burada muhasebe satışının e-ticaret siparişi olmadan yalnız negatif stok çıkışı oluşturduğunu doğruluyorum.
    [Fact]
    public void ApplyStockMovement_Should_Record_An_Orderless_Accounting_Sale_As_Outgoing()
    {
        var variant = CreateVariant(stock: 8);

        var movement = variant.ApplyStockMovement(
            -2,
            StockMovementType.AccountingSale,
            "Accounting sales order posted.");

        variant.Stock.Should().Be(6);
        movement.Type.Should().Be(StockMovementType.AccountingSale);
        movement.Direction.Should().Be(StockMovementDirection.Out);
        movement.QuantityDelta.Should().Be(-2);
        movement.StockBeforeMovement.Should().Be(8);
        movement.StockAfterMovement.Should().Be(6);
        movement.OrderId.Should().BeNull();
    }

    // Burada AccountingSale hareketinin e-ticaret Order veya Return referansı yerine yalnız Accounting mapping kullanmasını zorunlu tutuyorum.
    [Fact]
    public void ApplyStockMovement_Should_Reject_ECommerce_References_For_Accounting_Sale()
    {
        var variant = CreateVariant(stock: 8);

        Action withOrder = () => variant.ApplyStockMovement(
            -1,
            StockMovementType.AccountingSale,
            orderId: Guid.NewGuid());
        Action withReturn = () => variant.ApplyStockMovement(
            -1,
            StockMovementType.AccountingSale,
            returnRequestId: Guid.NewGuid());

        withOrder.Should().Throw<DomainException>();
        withReturn.Should().Throw<DomainException>();
        variant.Stock.Should().Be(8);
    }

    // Burada iptal hareketinin siparişe bağlı, pozitif imzalı stok girişi olduğunu doğruluyorum.
    [Fact]
    public void ApplyStockMovement_Should_Record_Cancellation_As_An_Incoming_Signed_Delta()
    {
        var variant = CreateVariant(stock: 6);
        var orderId = Guid.NewGuid();

        var movement = variant.ApplyStockMovement(
            2,
            StockMovementType.Cancellation,
            "Order cancelled.",
            orderId);

        variant.Stock.Should().Be(8);
        movement.Type.Should().Be(StockMovementType.Cancellation);
        movement.Direction.Should().Be(StockMovementDirection.In);
        movement.QuantityDelta.Should().Be(2);
        movement.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void ApplyStockMovement_Should_Record_Accounting_Cancellation_Without_ECommerce_Reference()
    {
        var variant = CreateVariant(stock: 6);

        var movement = variant.ApplyStockMovement(
            2,
            StockMovementType.AccountingSaleCancellation,
            "Accounting sales order cancelled.");

        variant.Stock.Should().Be(8);
        movement.Type.Should().Be(StockMovementType.AccountingSaleCancellation);
        movement.Direction.Should().Be(StockMovementDirection.In);
        movement.OrderId.Should().BeNull();
        movement.ReturnRequestId.Should().BeNull();
    }

    // Burada satış, iptal ve satış iadesi hareketlerinin zorunlu iş referansları olmadan oluşturulamadığını doğruluyorum.
    [Fact]
    public void ApplyStockMovement_Should_Require_The_Related_Business_Reference()
    {
        var variant = CreateVariant(stock: 4);

        Action saleWithoutOrder = () => variant.ApplyStockMovement(-1, StockMovementType.Sale);
        Action cancellationWithoutOrder = () => variant.ApplyStockMovement(1, StockMovementType.Cancellation);
        Action returnWithoutRequest = () => variant.ApplyStockMovement(1, StockMovementType.SaleReturn);

        saleWithoutOrder.Should().Throw<DomainException>();
        cancellationWithoutOrder.Should().Throw<DomainException>();
        returnWithoutRequest.Should().Throw<DomainException>();
    }

    // Burada sabit yönlü hareketlerin ters işaretle ve stok sınırlarını aşarak uygulanamadığını doğruluyorum.
    [Fact]
    public void ApplyStockMovement_Should_Reject_Invalid_Direction_And_Stock_Boundary_Violations()
    {
        var variant = CreateVariant(stock: 1);
        var fullVariant = CreateVariant(stock: int.MaxValue);
        var orderId = Guid.NewGuid();

        Action positiveSale = () => variant.ApplyStockMovement(1, StockMovementType.Sale, orderId: orderId);
        Action positiveAccountingSale = () => variant.ApplyStockMovement(1, StockMovementType.AccountingSale);
        Action negativeStock = () => variant.ApplyStockMovement(-2, StockMovementType.Sale, orderId: orderId);
        Action stockOverflow = () => fullVariant.ApplyStockMovement(1, StockMovementType.ManualAdjustment, "Overflow check");

        positiveSale.Should().Throw<DomainException>();
        positiveAccountingSale.Should().Throw<DomainException>();
        negativeStock.Should().Throw<DomainException>();
        stockOverflow.Should().Throw<DomainException>();
    }

    // Burada testlerin kullandığı geçerli stoklu varyantı oluşturuyorum.
    private static ProductVariant CreateVariant(int stock)
    {
        return new ProductVariant(1, "Standard", $"STOCK-{Guid.NewGuid():N}", 100m, stock);
    }
}
