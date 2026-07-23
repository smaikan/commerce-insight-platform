using ECommerce.Domain.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;

namespace ECommerce.UnitTests.Domain;

public sealed class OrderTests
{
    [Fact]
    public void Constructor_Should_Reject_Inconsistent_Grand_Total()
    {
        Action act = () => new Order(1, "ORD-1", 100, 10, 5, 20, 999);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangeStatus_Should_Allow_Valid_Order_Lifecycle()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order(1, "ORD-1", 100, 10, 5, 20, 115);

        order.ChangeStatus(OrderStatus.Confirmed, utcNow);
        order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, utcNow.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, utcNow.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, utcNow.AddMinutes(4));

        order.Status.Should().Be(OrderStatus.Delivered);
        order.PaidAt.Should().Be(utcNow.AddMinutes(1));
    }

    [Fact]
    public void ChangeStatus_Should_Reject_Invalid_Transition()
    {
        var utcNow = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order(1, "ORD-1", 100, 0, 0, 0, 100);

        Action act = () => order.ChangeStatus(OrderStatus.Delivered, utcNow);

        act.Should().Throw<DomainException>();
    }
}
