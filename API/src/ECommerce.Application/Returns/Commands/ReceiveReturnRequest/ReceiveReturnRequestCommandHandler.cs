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
    private readonly ReturnInventoryService _inventoryService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;

    // Burada fiziksel iade teslim alma akışının repository, stok, saat ve transaction bağımlılıklarını hazırlıyorum.
    public ReceiveReturnRequestCommandHandler(
        IReturnRequestRepository returnRequestRepository,
        ReturnInventoryService inventoryService,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IOrderNotificationService? notificationService = null)
    {
        _returnRequestRepository = returnRequestRepository;
        _inventoryService = inventoryService;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    // Burada teslim alınan refund ürünlerinin stok girişini aynı transaction içinde bir kez kaydediyorum.
    public Task<ReturnRequestDto> Handle(ReceiveReturnRequestCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => ReceiveInTransactionAsync(request.ReturnRequestId, transactionCancellationToken),
            cancellationToken);
    }

    // Burada exchange stok etkisini completion aşamasına bırakarak talebin teslim alındı geçişini uyguluyorum.
    private async Task<ReturnRequestDto> ReceiveInTransactionAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRequestRepository.GetByIdForUpdateAsync(returnRequestId, cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        returnRequest.Receive(_clock.UtcNow);
        if (returnRequest.Type == ReturnType.Refund)
        {
            await _inventoryService.RestockReceivedReturnAsync(returnRequest, cancellationToken);
        }

        if (_notificationService is not null)
        {
            await _notificationService.QueueReturnStatusChangedAsync(
                returnRequest,
                returnRequest.Order,
                cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return returnRequest.ToDto();
    }
}
