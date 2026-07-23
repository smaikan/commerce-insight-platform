using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class CartItemTests
{
    [Fact]
    public void TotalPrice_Should_Be_UnitPrice_Multiplied_By_Quantity()
    {
        var cartItem = new CartItem(
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            quantity: 3,
            unitPrice: 125.50m);

        cartItem.TotalPrice.Should().Be(376.50m);
    }

    [Fact]
    public void UpdateQuantity_Should_Update_TotalPrice()
    {
        var cartItem = new CartItem(
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            quantity: 2,
            unitPrice: 100m);

        cartItem.UpdateQuantity(4);

        cartItem.TotalPrice.Should().Be(400m);
    }

    [Fact]
    public void UpdateUnitPrice_Should_Update_TotalPrice()
    {
        var cartItem = new CartItem(
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            quantity: 2,
            unitPrice: 100m);

        cartItem.UpdateUnitPrice(150m);

        cartItem.TotalPrice.Should().Be(300m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateQuantity_Should_Reject_Invalid_Quantity(int quantity)
    {
        var cartItem = new CartItem(
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            quantity: 1,
            unitPrice: 100m);

        Action act = () => cartItem.UpdateQuantity(quantity);

        act.Should().Throw<DomainException>();
    }
}
