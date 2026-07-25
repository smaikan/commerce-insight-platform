using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Commands.ChangeOrderStatus;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Returns.Commands.ApproveReturnRequest;
using ECommerce.Application.Returns.Commands.CreateReturnRequest;
using ECommerce.Application.Returns.Commands.RejectReturnRequest;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ReturnOrderStatusApplicationTests
{
    // Burada yeni iade talebinin sipariş listesindeki durumu iade talebi oluşturuldu olarak değiştirdiğini doğruluyorum.
    [Fact]
    public async Task CreateReturnRequest_Should_Set_Order_Status_To_ReturnRequested()
    {
        var (order, orderItem) = CreateDeliveredOrder();
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                order.Id,
                order.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        returnRequests.Setup(repository => repository.AddAsync(
                It.IsAny<ReturnRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreateReturnRequestCommandHandler(
            orders.Object,
            returnRequests.Object,
            Mock.Of<IProductVariantRepository>(),
            new FixedCurrentUser(order.UserId),
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object);

        await handler.Handle(
            new CreateReturnRequestCommand(
                order.Id,
                ReturnType.Refund,
                [new CreateReturnItemCommand(orderItem.Id, 1)]),
            CancellationToken.None);

        order.Status.Should().Be(OrderStatus.ReturnRequested);
        order.ToSummaryDto().Status.Should().Be(OrderStatus.ReturnRequested);
    }

    // Burada yönetici onayının sipariş listesindeki durumu iade olarak değiştirdiğini doğruluyorum.
    [Fact]
    public async Task ApproveReturnRequest_Should_Set_Order_Status_To_ReturnApproved()
    {
        var clock = new FixedClock();
        var (order, orderItem) = CreateDeliveredOrder();
        var returnRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-APPROVE");
        order.MarkReturnRequested();
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByIdForUpdateAsync(
                returnRequest.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnRequest);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var handler = new ApproveReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            clock,
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object);

        await handler.Handle(
            new ApproveReturnRequestCommand(returnRequest.Id),
            CancellationToken.None);

        returnRequest.Status.Should().Be(ReturnRequestStatus.Approved);
        order.Status.Should().Be(OrderStatus.ReturnApproved);
        order.ToSummaryDto().Status.Should().Be(OrderStatus.ReturnApproved);
    }

    // Burada tek açık talep reddedildiğinde siparişin teslim edilmiş durumuna döndüğünü doğruluyorum.
    [Fact]
    public async Task RejectReturnRequest_Should_Restore_Delivered_When_No_Active_Request_Remains()
    {
        var clock = new FixedClock();
        var (order, orderItem) = CreateDeliveredOrder();
        var returnRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-REJECT");
        order.MarkReturnRequested();
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByIdForUpdateAsync(
                returnRequest.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnRequest);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([returnRequest]);
        var handler = new RejectReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            clock,
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object);

        await handler.Handle(
            new RejectReturnRequestCommand(returnRequest.Id),
            CancellationToken.None);

        returnRequest.Status.Should().Be(ReturnRequestStatus.Rejected);
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    // Burada başka onaylı talep varken bir talebin reddinin siparişin iade durumunu bozmadığını doğruluyorum.
    [Fact]
    public async Task RejectReturnRequest_Should_Keep_ReturnApproved_When_Another_Request_Is_Active()
    {
        var clock = new FixedClock();
        var (order, orderItem) = CreateDeliveredOrder();
        var rejectedRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-REJECTED");
        var approvedRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-APPROVED");
        approvedRequest.Approve(clock.UtcNow);
        order.MarkReturnApproved();
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByIdForUpdateAsync(
                rejectedRequest.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rejectedRequest);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([rejectedRequest, approvedRequest]);
        var handler = new RejectReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            clock,
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object);

        await handler.Handle(
            new RejectReturnRequestCommand(rejectedRequest.Id),
            CancellationToken.None);

        rejectedRequest.Status.Should().Be(ReturnRequestStatus.Rejected);
        order.Status.Should().Be(OrderStatus.ReturnApproved);
    }

    // Burada genel yönetim durum endpointinin iade akışına ait durumları doğrudan kabul etmediğini doğruluyorum.
    [Fact]
    public void ChangeOrderStatusValidator_Should_Reject_Return_Workflow_Statuses()
    {
        var validator = new ChangeOrderStatusCommandValidator();

        var requestedResult = validator.Validate(
            new ChangeOrderStatusCommand(Guid.NewGuid(), OrderStatus.ReturnRequested));
        var approvedResult = validator.Validate(
            new ChangeOrderStatusCommand(Guid.NewGuid(), OrderStatus.ReturnApproved));

        requestedResult.IsValid.Should().BeFalse();
        approvedResult.IsValid.Should().BeFalse();
    }

    // Burada geçerli teslim edilmiş sipariş ve kalemini iade akışı testleri için hazırlıyorum.
    private static (Order Order, OrderItem OrderItem) CreateDeliveredOrder()
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..30], 10m, 0m, 0m, 0m, 10m);
        var orderItem = order.AddItem(11, Guid.NewGuid(), "Returnable product", "RETURN-SKU", 10m, 1);
        order.EnsureItemsMatchSubTotal();
        order.ChangeStatus(OrderStatus.Confirmed, utcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, $"return_status_{Guid.NewGuid():N}");
        order.AddPayment(payment);
        payment.MarkAsPaid($"return_status_transaction_{Guid.NewGuid():N}");
        order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, utcNow.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, utcNow.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, utcNow.AddMinutes(4));
        return (order, orderItem);
    }

    // Burada onay veya ret testlerinde kullanılacak kalemli bekleyen refund talebini oluşturuyorum.
    private static ReturnRequest CreateRequestedRefund(Order order, OrderItem orderItem, string returnNumber)
    {
        var returnRequest = new ReturnRequest(order.Id, order.UserId, returnNumber, ReturnType.Refund);
        returnRequest.AddItem(orderItem, 1);
        return returnRequest;
    }

    // Burada serializable transaction delegesini testte doğrudan çalıştıracak birim işi hazırlıyorum.
    private static Mock<IUnitOfWork> CreateTransactionalUnitOfWork<TResponse>()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<TResponse>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<TResponse>>, CancellationToken>(
                (operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class FixedCurrentUser : ICurrentUserService
    {
        // Burada test için sabit kullanıcı kimliğini hazırlıyorum.
        public FixedCurrentUser(long userId)
        {
            UserId = userId;
        }

        public long? UserId { get; }
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
    }
}
