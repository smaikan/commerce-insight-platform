using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using ECommerce.Application.Returns.Dtos;
using MediatR;

namespace ECommerce.Application.Returns.Commands.ApproveReturnRequest;

public sealed class ApproveReturnRequestCommandHandler : IRequestHandler<ApproveReturnRequestCommand, ReturnRequestDto>
{
    private readonly IReturnRequestRepository _returnRequestRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;

    // Burada iade onayı iş akışının iade, sipariş, UTC saat ve transaction bağımlılıklarını hazırlıyorum.
    public ApproveReturnRequestCommandHandler(
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
        returnRequest.Approve(_clock.UtcNow, request.DecisionNote);
        order.MarkReturnApproved();
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
