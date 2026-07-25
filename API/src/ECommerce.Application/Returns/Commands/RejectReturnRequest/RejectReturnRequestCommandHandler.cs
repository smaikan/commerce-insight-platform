using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Returns.Dtos;
using ECommerce.Application.Returns.Services;
using MediatR;

namespace ECommerce.Application.Returns.Commands.RejectReturnRequest;

public sealed class RejectReturnRequestCommandHandler : IRequestHandler<RejectReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;

    // Burada iade ret iş akışının iade, sipariş, UTC saat ve transaction bağımlılıklarını hazırlıyorum.
    public RejectReturnRequestCommandHandler(
        IReturnRequestRepository returnRequestRepository,
        IOrderRepository orderRepository,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOrderNotificationService? notificationService = null)
    {
        _returnRequestRepository = returnRequestRepository;
        _orderRepository = orderRepository;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    // Burada yöneticinin ret geçişini kalıcı olarak kaydediyorum.
    public Task<ReturnRequestDto> Handle(RejectReturnRequestCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => RejectInTransactionAsync(request, transactionCancellationToken),
            cancellationToken);
    }

    // Burada aynı iade talebine eşzamanlı karar verilmesini engelleyen transaction içinde ret ve bildirimi kaydediyorum.
    private async Task<ReturnRequestDto> RejectInTransactionAsync(
        RejectReturnRequestCommand request,
        CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRequestRepository.GetByIdForUpdateAsync(
            request.ReturnRequestId,
            cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        var order = await _orderRepository.GetByIdForUpdateAsync(returnRequest.OrderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
        returnRequest.Reject(_clock.UtcNow, request.DecisionNote);
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
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return returnRequest.ToDto();
    }
}
