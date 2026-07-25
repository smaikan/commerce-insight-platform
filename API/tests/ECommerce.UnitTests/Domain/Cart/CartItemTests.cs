using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using FluentAssertions;
using CartEntity = ECommerce.Domain.Entities.Cart;

namespace ECommerce.UnitTests.Domain.Cart;

public sealed class CartItemTests
{
    // Burada satır toplamının birim fiyat ile adedin çarpımından oluştuğunu doğruluyorum.
    [Fact]
    public void TotalPrice_Should_Be_UnitPrice_Multiplied_By_Quantity()
    {
        var cart = CartEntity.CreateForUser(1);
        var cartItem = cart.AddItem(
            productId: 10,
            productVariantId: Guid.NewGuid(),
            quantity: 3,
            unitPrice: 125.50m);

        cartItem.TotalPrice.Should().Be(376.50m);
    }

    // Burada sıfır veya negatif adetle sepet satırı oluşturulmasını engelliyorum.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_Should_Reject_Invalid_Quantity(int quantity)
    {
        var cart = CartEntity.CreateForUser(1);

        Action act = () => cart.AddItem(
            productId: 10,
            productVariantId: Guid.NewGuid(),
            quantity,
            unitPrice: 100m);

        act.Should().Throw<DomainException>();
        cart.Items.Should().BeEmpty();
    }

    // Burada sıfır veya negatif fiyatla sepet satırı oluşturulmasını engelliyorum.
    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("0.001")]
    [InlineData("10000000000000000")]
    public void AddItem_Should_Reject_Invalid_UnitPrice(string unitPriceText)
    {
        var cart = CartEntity.CreateForUser(1);
        var unitPrice = decimal.Parse(unitPriceText, System.Globalization.CultureInfo.InvariantCulture);

        Action act = () => cart.AddItem(
            productId: 10,
            productVariantId: Guid.NewGuid(),
            quantity: 1,
            unitPrice);

        act.Should().Throw<DomainException>();
        cart.Items.Should().BeEmpty();
    }

    // Burada ürün ve varyant kimliklerinden biri geçersizse satır oluşturulmadığını doğruluyorum.
    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(1, true)]
    public void AddItem_Should_Reject_Invalid_Product_Identifiers(
        long productId,
        bool useEmptyVariantId)
    {
        var cart = CartEntity.CreateForUser(1);
        var productVariantId = useEmptyVariantId ? Guid.Empty : Guid.NewGuid();

        Action act = () => cart.AddItem(
            productId,
            productVariantId,
            quantity: 1,
            unitPrice: 100m);

        act.Should().Throw<DomainException>();
        cart.Items.Should().BeEmpty();
    }

    // Burada adet artışı int sınırını aşarsa mevcut satırın değişmeden kaldığını doğruluyorum.
    [Fact]
    public void AddItem_Should_Reject_Quantity_Overflow_Without_Changing_Item()
    {
        var cart = CartEntity.CreateForUser(1);
        var productVariantId = Guid.NewGuid();
        var item = cart.AddItem(10, productVariantId, int.MaxValue, 1m);
        var concurrencyToken = cart.ConcurrencyToken;

        Action act = () => cart.AddItem(10, productVariantId, 1, 2m);

        act.Should().Throw<DomainException>();
        item.Quantity.Should().Be(int.MaxValue);
        item.UnitPrice.Should().Be(1m);
        cart.ConcurrencyToken.Should().Be(concurrencyToken);
    }

    // Burada satır toplamı desteklenen para alanını aşacaksa geçersiz durumun oluşmasını engelliyorum.
    [Fact]
    public void AddItem_Should_Reject_TotalPrice_Outside_Monetary_Limit()
    {
        var cart = CartEntity.CreateForUser(1);

        Action act = () => cart.AddItem(
            productId: 10,
            productVariantId: Guid.NewGuid(),
            quantity: 2,
            unitPrice: CartItem.MaximumSupportedAmount);

        act.Should().Throw<DomainException>();
        cart.Items.Should().BeEmpty();
    }

    // Burada desteklenen en yüksek para değerinin sınırda geçerli kaldığını doğruluyorum.
    [Fact]
    public void AddItem_Should_Accept_Maximum_Supported_Amount()
    {
        var cart = CartEntity.CreateForUser(1);

        var item = cart.AddItem(
            productId: 10,
            productVariantId: Guid.NewGuid(),
            quantity: 1,
            unitPrice: CartItem.MaximumSupportedAmount);

        item.TotalPrice.Should().Be(CartItem.MaximumSupportedAmount);
        cart.SubTotal.Should().Be(CartItem.MaximumSupportedAmount);
    }
}
