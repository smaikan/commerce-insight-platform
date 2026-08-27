using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Payments;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Orders.Dtos;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Orders.Commands.CreatePayment;

public sealed class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IReadOnlyCollection<IPaymentGateway> _paymentGateways;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;
    private readonly IAuthoritativeSalesMetricService _salesMetrics;

    // Burada ödeme denemesi için sipariş, sağlayıcı, kullanıcı, saat ve transaction bağımlılıklarını hazırlıyorum.
    public CreatePaymentCommandHandler(
        IOrderRepository orderRepository,
        IEnumerable<IPaymentGateway> paymentGateways,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IAuthoritativeSalesMetricService salesMetrics,
        IOrderNotificationService? notificationService = null)
    {
        _orderRepository = orderRepository;
        _paymentGateways = paymentGateways.ToList();
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _salesMetrics = salesMetrics;
        _notificationService = notificationService;
    }

    // Burada tek idempotency anahtarı için önce kalıcı bekleyen denemeyi oluşturup sağlayıcıyı transaction dışında çağırıyorum.
    public async Task<PaymentDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.GetRequiredUserId();
        var idempotencyKey = Payment.NormalizeIdempotencyKey(request.IdempotencyKey);
        var gateway = _paymentGateways.SingleOrDefault(candidate => candidate.Provider == request.Provider)
            ?? throw new ConflictException("The selected payment provider is not configured.");
        PaymentDto? existingPayment = null;
        Guid? createdPaymentId = null;
        decimal paymentAmount = 0m;

        var wasCreated = await _unitOfWork.ExecuteInSerializableTransactionAsync(
            async transactionCancellationToken =>
            {
                var order = await _orderRepository.GetByIdForUserForUpdateAsync(
                    request.OrderId,
                    userId,
                    transactionCancellationToken)
                    ?? throw new NotFoundException("Order was not found.");
                var matchingPayment = order.Payments.SingleOrDefault(payment =>
                    string.Equals(payment.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));
                if (matchingPayment is not null)
                {
                    existingPayment = matchingPayment.ToDto();
                    return false;
                }

                if (order.GrandTotal == 0m)
                {
                    throw new ConflictException("This order does not require a payment.");
                }

                if (order.Status is not OrderStatus.Pending and not OrderStatus.Confirmed)
                {
                    throw new ConflictException("This order cannot accept another payment attempt.");
                }

                if (order.Payments.Any(payment => payment.Status == PaymentStatus.Pending))
                {
                    throw new ConflictException("Another payment attempt is still being processed.");
                }

                var payment = new Payment(order.Id, request.Provider, order.GrandTotal, idempotencyKey);
                order.AddPayment(payment);
                await _orderRepository.AddPaymentAsync(payment, transactionCancellationToken);
                createdPaymentId = payment.Id;
                paymentAmount = payment.Amount;
                await _unitOfWork.SaveChangesAsync(transactionCancellationToken);
                return true;
            },
            cancellationToken);

        if (!wasCreated)
        {
            return existingPayment
                ?? throw new ConflictException("A payment attempt could not be resolved.");
        }

        var paymentId = createdPaymentId
            ?? throw new ConflictException("A payment attempt could not be created.");
        PaymentGatewayResult gatewayResult;
        try
        {
            gatewayResult = await gateway.ChargeAsync(
                new PaymentGatewayRequest(request.OrderId, paymentAmount, idempotencyKey),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            gatewayResult = new PaymentGatewayResult(
                false,
                null,
                "Payment provider communication failed.");
        }

        return await _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => CompletePaymentInTransactionAsync(
                request.OrderId,
                userId,
                paymentId,
                gatewayResult,
                transactionCancellationToken),
            cancellationToken);
    }

    // Burada sağlayıcı sonucunu yalnız aynı bekleyen denemeye uygulayıp sipariş ve ödeme durumlarını atomik olarak güncelliyorum.
    private async Task<PaymentDto> CompletePaymentInTransactionAsync(
        Guid orderId,
        long userId,
        Guid paymentId,
        PaymentGatewayResult gatewayResult,
        CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUserForUpdateAsync(orderId, userId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        var payment = order.Payments.SingleOrDefault(candidate => candidate.Id == paymentId)
            ?? throw new ConflictException("Payment attempt was not found.");
        if (payment.Status != PaymentStatus.Pending)
        {
            return payment.ToDto();
        }

        if (order.Status != OrderStatus.Confirmed)
        {
            throw new ConflictException("Payment result requires reconciliation before it can be applied.");
        }

        if (gatewayResult.Succeeded)
        {
            var transactionId = gatewayResult.TransactionId;
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                throw new ConflictException("Payment result requires reconciliation before it can be applied.");
            }

            payment.MarkAsPaid(transactionId);
            order.ChangeStatus(OrderStatus.Paid, _clock.UtcNow);
            await _salesMetrics.RecordPaidOrderAsync(order, cancellationToken);
        }
        else
        {
            payment.MarkAsFailed("Payment provider rejected the payment attempt.");
        }

        if (_notificationService is not null)
        {
            await _notificationService.QueuePaymentResultAsync(order, payment, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return payment.ToDto();
    }
}
