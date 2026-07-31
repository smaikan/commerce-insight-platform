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
    private readonly ReturnInventoryService _inventoryService;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderNotificationService? _notificationService;

    // Burada iade tamamlama akışının repository, stok, saat ve transaction bağımlılıklarını hazırlıyorum.
    public CompleteReturnRequestCommandHandler(
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

    // Burada değişim replacement stok işlemini iade stok girişinin olduğu transaction içinde tamamlayıp talebi kapatıyorum.
    public Task<ReturnRequestDto> Handle(CompleteReturnRequestCommand request, CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInSerializableTransactionAsync(
            transactionCancellationToken => CompleteInTransactionAsync(request.ReturnRequestId, transactionCancellationToken),
            cancellationToken);
    }

    // Burada refund için finansal kapanışı kaydedip exchange için stokları atomik uyguluyorum.
    private async Task<ReturnRequestDto> CompleteInTransactionAsync(Guid returnRequestId, CancellationToken cancellationToken)
    {
        var returnRequest = await _returnRequestRepository.GetByIdForUpdateAsync(returnRequestId, cancellationToken)
            ?? throw new NotFoundException("Return request was not found.");
        if (returnRequest.Type == ReturnType.Exchange)
        {
            await _inventoryService.FulfillExchangeAsync(returnRequest, cancellationToken);
        }

        returnRequest.Complete(_clock.UtcNow);
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
