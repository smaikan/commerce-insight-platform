using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Orders.Services;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Commands.ChangeOrderStatus;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Returns.Commands.ApproveReturnRequest;
using ECommerce.Application.Returns.Commands.CompleteReturnRequest;
using ECommerce.Application.Returns.Commands.CreateReturnRequest;
using ECommerce.Application.Returns.Commands.RejectReturnRequest;
using ECommerce.Application.Returns.Commands.ReceiveReturnRequest;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Application.Returns.Services;
using ECommerce.Domain.Common;
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
        var (order, orderItem, _) = CreateDeliveredOrder();
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                order.Id,
                order.UserId!.Value,
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
            new FixedCurrentUser(order.UserId!.Value),
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

    // Burada fiziksel teslimin siparişi ReturnRequested tutup stok ve sipariş durum bildirimi üretmediğini doğruluyorum.
    [Fact]
    public async Task ReceiveReturnRequest_Should_Keep_Order_ReturnRequested_Without_Stock_Effect()
    {
        var clock = new FixedClock();
        var (order, orderItem, originalVariant) = CreateDeliveredOrder();
        var returnRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-RECEIVE");
        order.MarkReturnRequested();
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByIdForUpdateAsync(
                returnRequest.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnRequest);
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([returnRequest]);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var variants = new Mock<IProductVariantRepository>(MockBehavior.Strict);
        var notifications = new Mock<IOrderNotificationService>();
        notifications.Setup(service => service.QueueReturnStatusChangedAsync(
                returnRequest,
                order,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new ReceiveReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            new ReturnInventoryService(variants.Object),
            clock,
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object,
            notifications.Object);

        await handler.Handle(new ReceiveReturnRequestCommand(returnRequest.Id), CancellationToken.None);

        returnRequest.Status.Should().Be(ReturnRequestStatus.Received);
        returnRequest.IsAwaitingDecision().Should().BeTrue();
        order.Status.Should().Be(OrderStatus.ReturnRequested);
        originalVariant.Stock.Should().Be(3);
        originalVariant.StockMovements.Should().NotContain(movement =>
            movement.Type == StockMovementType.SaleReturn);
        notifications.Verify(service => service.QueueOrderStatusChangedAsync(
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada refund onayının sipariş listesindeki kalıcı durumu ücret iadesi olarak değiştirdiğini doğruluyorum.
    [Fact]
    public async Task ApproveRefundReturnRequest_Should_Set_Order_Status_To_Refunded()
    {
        var clock = new FixedClock();
        var (order, orderItem, originalVariant) = CreateDeliveredOrder();
        var returnRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-APPROVE");
        returnRequest.Receive(clock.UtcNow);
        order.MarkReturnRequested();
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByIdForUpdateAsync(
                returnRequest.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnRequest);
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([returnRequest]);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var unitOfWork = CreateTransactionalUnitOfWork<ReturnRequestDto>();
        var salesMetrics = new Mock<IAuthoritativeSalesMetricService>();
        var handler = new ApproveReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            CreateInventoryService(originalVariant),
            clock,
            unitOfWork.Object,
            salesMetrics.Object);

        await handler.Handle(
            new ApproveReturnRequestCommand(returnRequest.Id),
            CancellationToken.None);
        Func<Task> repeatedApproval = () => handler.Handle(
            new ApproveReturnRequestCommand(returnRequest.Id),
            CancellationToken.None);

        returnRequest.Status.Should().Be(ReturnRequestStatus.Approved);
        order.Status.Should().Be(OrderStatus.Refunded);
        originalVariant.Stock.Should().Be(4);
        originalVariant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.SaleReturn);
        await repeatedApproval.Should().ThrowAsync<ReturnStatusTransitionException>();
        order.ToSummaryDto().Status.Should().Be(OrderStatus.Refunded);
        salesMetrics.Verify(metric => metric.ReverseApprovedRefundAsync(
            order,
            returnRequest,
            It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada exchange onayının siparişin iade onayı durumunu koruduğunu doğruluyorum.
    [Fact]
    public async Task ApproveExchangeReturnRequest_Should_Set_Order_Status_To_ReturnApproved()
    {
        var clock = new FixedClock();
        var (order, orderItem, originalVariant) = CreateDeliveredOrder();
        var replacementVariant = new ProductVariant(
            originalVariant.ProductId,
            "Replacement",
            $"REPLACEMENT-{Guid.NewGuid():N}",
            originalVariant.Price,
            3);
        var returnRequest = new ReturnRequest(order.Id, order.UserId, "RET-ORDER-EXCHANGE", ReturnType.Exchange);
        returnRequest.AddItem(orderItem, 1, replacementVariant.Id);
        returnRequest.Receive(clock.UtcNow);
        order.MarkReturnRequested();
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByIdForUpdateAsync(returnRequest.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnRequest);
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([returnRequest]);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var salesMetrics = new Mock<IAuthoritativeSalesMetricService>();
        var handler = new ApproveReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            CreateInventoryService(originalVariant, replacementVariant),
            clock,
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object,
            salesMetrics.Object);

        await handler.Handle(new ApproveReturnRequestCommand(returnRequest.Id), CancellationToken.None);

        returnRequest.Status.Should().Be(ReturnRequestStatus.Approved);
        order.Status.Should().Be(OrderStatus.ReturnApproved);
        originalVariant.Stock.Should().Be(4);
        replacementVariant.Stock.Should().Be(2);
        salesMetrics.Verify(metric => metric.ReverseApprovedRefundAsync(
            It.IsAny<Order>(),
            It.IsAny<ReturnRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada deployment öncesi onaylı exchange kaydının receive-complete uyumluluğunda stokları bir kez uyguladığını doğruluyorum.
    [Fact]
    public async Task CompleteReturnRequest_Should_Fulfill_Only_Legacy_Received_Exchange()
    {
        var clock = new FixedClock();
        var (order, orderItem, originalVariant) = CreateDeliveredOrder();
        var replacementVariant = new ProductVariant(
            originalVariant.ProductId,
            "Legacy replacement",
            $"LEGACY-{Guid.NewGuid():N}",
            originalVariant.Price,
            2);
        var returnRequest = new ReturnRequest(order.Id, order.UserId, "RET-LEGACY-COMPLETE", ReturnType.Exchange);
        returnRequest.AddItem(orderItem, 1, replacementVariant.Id);
        SetPrivateProperty(returnRequest, nameof(ReturnRequest.Status), ReturnRequestStatus.Approved);
        SetPrivateProperty(returnRequest, nameof(ReturnRequest.ApprovedAt), clock.UtcNow.AddMinutes(-2));
        returnRequest.Receive(clock.UtcNow.AddMinutes(-1));
        order.MarkReturnRequested();
        order.MarkReturnApproved();
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByIdForUpdateAsync(
                returnRequest.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnRequest);
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(
                order.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([returnRequest]);
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var handler = new CompleteReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            CreateInventoryService(originalVariant, replacementVariant),
            clock,
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object);

        await handler.Handle(new CompleteReturnRequestCommand(returnRequest.Id), CancellationToken.None);

        returnRequest.Status.Should().Be(ReturnRequestStatus.Completed);
        order.Status.Should().Be(OrderStatus.ReturnApproved);
        originalVariant.Stock.Should().Be(4);
        replacementVariant.Stock.Should().Be(1);
    }

    // Burada kısmi bir refund onayından sonra kalan adet için yeni talep açılırken siparişin ücret iadesi durumunu koruduğunu doğruluyorum.
    [Fact]
    public async Task CreateReturnRequest_Should_Allow_Remaining_Quantity_After_Partial_Refund()
    {
        var (order, orderItem, _) = CreateDeliveredOrder(2);
        var approvedRefund = CreateRequestedRefund(order, orderItem, "RET-PARTIAL-REFUND");
        approvedRefund.Receive(new FixedClock().UtcNow);
        approvedRefund.Approve(new FixedClock().UtcNow);
        order.MarkRefunded();
        var orders = new Mock<IOrderRepository>();
        orders.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                order.Id,
                order.UserId!.Value,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var returnRequests = new Mock<IReturnRequestRepository>();
        returnRequests.Setup(repository => repository.GetByOrderIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([approvedRefund]);
        returnRequests.Setup(repository => repository.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new CreateReturnRequestCommandHandler(
            orders.Object,
            returnRequests.Object,
            Mock.Of<IProductVariantRepository>(),
            new FixedCurrentUser(order.UserId!.Value),
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object);

        await handler.Handle(
            new CreateReturnRequestCommand(
                order.Id,
                ReturnType.Refund,
                [new CreateReturnItemCommand(orderItem.Id, 1)]),
            CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Refunded);
        returnRequests.Verify(repository => repository.AddAsync(It.IsAny<ReturnRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada tek açık talep reddedildiğinde siparişin teslim edilmiş durumuna döndüğünü doğruluyorum.
    [Fact]
    public async Task RejectReturnRequest_Should_Restore_Delivered_When_No_Active_Request_Remains()
    {
        var clock = new FixedClock();
        var (order, orderItem, _) = CreateDeliveredOrder();
        var returnRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-REJECT");
        returnRequest.Receive(clock.UtcNow);
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
        var notifications = CreateNotificationService(returnRequest, order);
        var handler = new RejectReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            clock,
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object,
            notifications.Object);

        await handler.Handle(
            new RejectReturnRequestCommand(returnRequest.Id),
            CancellationToken.None);

        returnRequest.Status.Should().Be(ReturnRequestStatus.Rejected);
        order.Status.Should().Be(OrderStatus.Delivered);
        notifications.Verify(service => service.QueueReturnStatusChangedAsync(
            returnRequest,
            order,
            It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(service => service.QueueOrderStatusChangedAsync(
            order,
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada başka onaylı talep varken bir talebin reddinin siparişin iade durumunu bozmadığını doğruluyorum.
    [Fact]
    public async Task RejectReturnRequest_Should_Keep_ReturnApproved_When_Another_Request_Is_Active()
    {
        var clock = new FixedClock();
        var (order, orderItem, _) = CreateDeliveredOrder();
        var rejectedRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-REJECTED");
        rejectedRequest.Receive(clock.UtcNow);
        var approvedRequest = CreateRequestedRefund(order, orderItem, "RET-ORDER-APPROVED");
        approvedRequest.Receive(clock.UtcNow);
        approvedRequest.Approve(clock.UtcNow);
        order.MarkRefunded();
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
        var notifications = CreateNotificationService(rejectedRequest, order);
        var handler = new RejectReturnRequestCommandHandler(
            returnRequests.Object,
            orders.Object,
            clock,
            CreateTransactionalUnitOfWork<ReturnRequestDto>().Object,
            notifications.Object);

        await handler.Handle(
            new RejectReturnRequestCommand(rejectedRequest.Id),
            CancellationToken.None);

        rejectedRequest.Status.Should().Be(ReturnRequestStatus.Rejected);
        order.Status.Should().Be(OrderStatus.Refunded);
        notifications.Verify(service => service.QueueReturnStatusChangedAsync(
            rejectedRequest,
            order,
            It.IsAny<CancellationToken>()), Times.Once);
        notifications.Verify(service => service.QueueOrderStatusChangedAsync(
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()), Times.Never);
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
    private static (Order Order, OrderItem OrderItem, ProductVariant Variant) CreateDeliveredOrder(int quantity = 1)
    {
        var utcNow = new DateTime(2026, 7, 25, 12, 0, 0, DateTimeKind.Utc);
        var total = 10m * quantity;
        var variant = new ProductVariant(11, "Original", $"RETURN-{Guid.NewGuid():N}", 10m, 3);
        var order = new Order(7, $"ORD-{Guid.NewGuid():N}"[..30], total, 0m, 0m, 0m, total);
        var orderItem = order.AddItem(11, variant.Id, "Returnable product", variant.Sku, 10m, quantity);
        order.EnsureItemsMatchSubTotal();
        order.ChangeStatus(OrderStatus.Confirmed, utcNow);
        var payment = new Payment(order.Id, PaymentProvider.Fake, order.GrandTotal, $"return_status_{Guid.NewGuid():N}");
        order.AddPayment(payment);
        payment.MarkAsPaid($"return_status_transaction_{Guid.NewGuid():N}");
        order.ChangeStatus(OrderStatus.Paid, utcNow.AddMinutes(1));
        order.ChangeStatus(OrderStatus.Preparing, utcNow.AddMinutes(2));
        order.ChangeStatus(OrderStatus.Shipped, utcNow.AddMinutes(3));
        order.ChangeStatus(OrderStatus.Delivered, utcNow.AddMinutes(4));
        return (order, orderItem, variant);
    }

    // Burada onay veya ret testlerinde kullanılacak kalemli bekleyen refund talebini oluşturuyorum.
    private static ReturnRequest CreateRequestedRefund(Order order, OrderItem orderItem, string returnNumber)
    {
        var returnRequest = new ReturnRequest(order.Id, order.UserId, returnNumber, ReturnType.Refund);
        returnRequest.AddItem(orderItem, 1);
        return returnRequest;
    }

    // Burada onay handler testinin stok varyantlarını kilitli repository sonucu olarak hazırlıyorum.
    private static ReturnInventoryService CreateInventoryService(params ProductVariant[] variants)
    {
        var repository = new Mock<IProductVariantRepository>();
        repository.Setup(candidate => candidate.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(variants);
        return new ReturnInventoryService(repository.Object);
    }

    // Burada iade durum bildirimini kabul eden ve sipariş bildirimi çağrılarını doğrulamaya açık mock servisi hazırlıyorum.
    private static Mock<IOrderNotificationService> CreateNotificationService(ReturnRequest returnRequest, Order order)
    {
        var notifications = new Mock<IOrderNotificationService>();
        notifications.Setup(service => service.QueueReturnStatusChangedAsync(
                returnRequest,
                order,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        notifications.Setup(service => service.QueueOrderStatusChangedAsync(
                order,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return notifications;
    }

    // Burada yalnız legacy fixture oluşturmak için ReturnRequest'in EF tarafından doldurulan private alanını ayarlıyorum.
    private static void SetPrivateProperty<T>(ReturnRequest returnRequest, string propertyName, T value)
    {
        typeof(ReturnRequest).GetProperty(propertyName)!.SetValue(returnRequest, value);
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
