using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Application.Returns.Services;
using ECommerce.Application.Orders.Services;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Returns.Commands.ApproveReturnRequest;

public sealed class ApproveReturnRequestCommandHandler : IRequestHandler<ApproveReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ReturnInventoryService _inventoryService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;
    private readonly IAuthoritativeSalesMetricService _salesMetrics;

    // Burada iade onayının iade, sipariş, stok, UTC saat ve transaction bağımlılıklarını hazırlıyorum.
    public ApproveReturnRequestCommandHandler(
        IReturnRequestRepository returnRequestRepository,
        IOrderRepository orderRepository,
        ReturnInventoryService inventoryService,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IAuthoritativeSalesMetricService salesMetrics,
        IOrderNotificationService? notificationService = null)
    {
        _returnRequestRepository = returnRequestRepository;
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _salesMetrics = salesMetrics;
        _notificationService = notificationService;
    }

    // Burada yöneticinin onay geçişini kalıcı olarak kaydediyorum.
    public Task<ReturnRequestDto> Handle(ApproveReturnRequestCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => ApproveInTransactionAsync(request, transactionCancellationToken),
            cancellationToken);
    }

    // Burada aynı iade talebine eşzamanlı karar verilmesini engelleyen transaction içinde onay ve bildirimi kaydediyorum.
    private async Task<ReturnRequestDto> ApproveInTransactionAsync(
        ApproveReturnRequestCommand request,
        CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRequestRepository.GetByIdForUpdateAsync(
            request.ReturnRequestId,
            cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        var order = await _orderRepository.GetByIdForUpdateAsync(returnRequest.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        var previousOrderStatus = order.Status;
        returnRequest.Approve(_clock.UtcNow, request.DecisionNote);
        if (returnRequest.Type == ReturnType.Refund)
        {
            await _inventoryService.RestockRefundAsync(returnRequest, cancellationToken);
        }
        else
        {
            await _inventoryService.FulfillExchangeAsync(returnRequest, cancellationToken);
        }

        var returnRequests = await _returnRequestRepository.GetByOrderIdForUpdateAsync(
            returnRequest.OrderId,
            cancellationToken);
        ReturnOrderStatusSynchronizer.Synchronize(order, returnRequests);
        if (returnRequest.Type == ReturnType.Refund)
        {
            await _salesMetrics.ReverseApprovedRefundAsync(order, returnRequest, cancellationToken);
        }

        if (_notificationService is not null)
        {
            await _notificationService.QueueReturnStatusChangedAsync(
                returnRequest,
                order,
                cancellationToken);
            if (order.Status != previousOrderStatus)
            {
                await _notificationService.QueueOrderStatusChangedAsync(order, cancellationToken);
            }
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return returnRequest.ToDto();
    }
}
