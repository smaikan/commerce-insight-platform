using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Application.Returns.Services;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Returns.Commands.ReceiveReturnRequest;

public sealed class ReceiveReturnRequestCommandHandler : IRequestHandler<ReceiveReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ReturnInventoryService _inventoryService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;

    // Burada fiziksel teslim alma akışının iade, sipariş, legacy stok, saat ve transaction bağımlılıklarını hazırlıyorum.
    public ReceiveReturnRequestCommandHandler(
        IReturnRequestRepository returnRequestRepository,
        IOrderRepository orderRepository,
        ReturnInventoryService inventoryService,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOrderNotificationService? notificationService = null)
    {
        _returnRequestRepository = returnRequestRepository;
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    // Burada fiziksel teslimi karar öncesi kaydedip yeni akışta herhangi bir stok etkisi oluşturmuyorum.
    public Task<ReturnRequestDto> Handle(ReceiveReturnRequestCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => ReceiveInTransactionAsync(request.ReturnRequestId, transactionCancellationToken),
            cancellationToken);
    }

    // Burada yeni teslimi karara hazırlar, yalnız eski onaylı refund kaydının önceki stok davranışını uyumlu tutuyorum.
    private async Task<ReturnRequestDto> ReceiveInTransactionAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRequestRepository.GetByIdForUpdateAsync(returnRequestId, cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        var order = await _orderRepository.GetByIdForUpdateAsync(returnRequest.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        var previousOrderStatus = order.Status;
        returnRequest.Receive(_clock.UtcNow);
        if (returnRequest.Type == ReturnType.Refund && returnRequest.IsLegacyReceivedAwaitingCompletion())
        {
            await _inventoryService.RestockRefundAsync(returnRequest, cancellationToken);
        }

        var returnRequests = await _returnRequestRepository.GetByOrderIdForUpdateAsync(
            returnRequest.OrderId,
            cancellationToken);
        ReturnOrderStatusSynchronizer.Synchronize(order, returnRequests);

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
