using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Application.Returns.Services;
using ECommerce.Domain.Enums;
using MediatR;

namespace ECommerce.Application.Returns.Commands.CompleteReturnRequest;

public sealed class CompleteReturnRequestCommandHandler : IRequestHandler<CompleteReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ReturnInventoryService _inventoryService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;

    // Burada legacy tamamlama akışının iade, sipariş, stok, saat ve transaction bağımlılıklarını hazırlıyorum.
    public CompleteReturnRequestCommandHandler(
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

    // Burada yalnız eski yaşam döngüsündeki kaydın uyumlu completion işlemini aynı transaction içinde kapatıyorum.
    public Task<ReturnRequestDto> Handle(CompleteReturnRequestCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => CompleteInTransactionAsync(request.ReturnRequestId, transactionCancellationToken),
            cancellationToken);
    }

    // Burada eski exchange stoklarını atomik uygulayıp sipariş durumunu diğer aktif taleplerle yeniden türetiyorum.
    private async Task<ReturnRequestDto> CompleteInTransactionAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRequestRepository.GetByIdForUpdateAsync(returnRequestId, cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        var order = await _orderRepository.GetByIdForUpdateAsync(returnRequest.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        var previousOrderStatus = order.Status;
        returnRequest.Complete(_clock.UtcNow);
        if (returnRequest.Type == ReturnType.Exchange)
        {
            await _inventoryService.FulfillExchangeAsync(returnRequest, cancellationToken);
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
